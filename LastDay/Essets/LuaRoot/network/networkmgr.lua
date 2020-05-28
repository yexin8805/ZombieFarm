--
-- @file    network/networkmgr.lua
-- @anthor  xing weizhen (xingweizhen@rongygame.com)
-- @date    2015-04-17 19:07:04
-- @desc    网络消息管理
--

local tostring, pairs, table, string
    = tostring, pairs, table, string
local libunity = require "libunity.cs"

local UE_Time = UE.Time

local HTTP_Silent = {
    report = true,
    VERSION = true,
}
local CONNECT_TIMEOUT = 10
local ClientDEF = _G.DEF.Client
local MainCli = ClientDEF.get("GameTcp")
local GameCli = MainCli
-- HTTP回调
local HTTPHandler = {}
-- 下载回调
local DownloadCbf = {}
-- 重连次数
local _lastHeartTime = 0

local P = {
    MainCli = MainCli,
}

function P.set_cli(cli)
    GameCli = cli or MainCli
end

function P.get_tcp() return GameCli.tcp end

function P.get(name)
    return ClientDEF.get(name)
end

-- 回调方法
function P.on_nc_init(mgr)
    GameCli:initialize()
    GameCli:set_handler(P.on_nc_receiving)
end

function P.on_nc_receiving(cli, nm)
    if cli == GameCli then
        _lastHeartTime = UE_Time.realtimeSinceStartup
    end
end

function P.on_www(url, tag, resp, isDone, err)
    _G.UI.Waiting.hide()
    local cbf = HTTPHandler[tag]
    if err then
        if not HTTP_Silent[tag] then _G.UI.Toast.norm(_G.TEXT.tipConnectFailure) end
        libunity.LogW("Http Fail: [{0}:{1}]{2}; {3}", tag, url, resp, err)
    elseif not isDone then
        if not HTTP_Silent[tag] then _G.UI.Toast.norm(_G.TEXT.tipConnectTimeout) end
        libunity.LogW("Http Timeout: [{0}:{1}]{2}", tag, url, resp)
    end
    if cbf then cbf(resp, isDone, err) end
end

function P.on_download(url, current, total, isDone, err)
    if err then libunity.LogW("Download from {0} Error:{1}", url, err) end

    local cbf = DownloadCbf[url]
    if cbf then cbf(url, current, total, isDone, err) end
end

function P.check_state(tm)
    local logined = rawget(DY_DATA, "Player") ~= nil
    if not logined then return true end

    if GameCli:connected() then
        -- 尝试发送心跳
        local currTime = UE_Time.realtimeSinceStartup
        if currTime - _lastHeartTime > 30 then
            _lastHeartTime = currTime
            P.send(P.msg("CS_KEEP_HEART"))
        end
    elseif GameCli.host then
        libunity.LogW("网络未连接...")
        if GameCli:get_error() ~= nil then
            GameCli:reconnect()
        end
    end
end

-- ============================================================================

-- 启动一个HTTP POST
function P.http_post(tag, url, postData, headers, cbf)
    libnetwork.HttpPost(tag, url, postData, headers, CONNECT_TIMEOUT);
    if cbf then HTTPHandler[tag] = cbf end
end

-- 启动一个HTTP GET
function P.http_get(tag, url, param, cbf)
    libnetwork.HttpGet(tag, url, param, CONNECT_TIMEOUT);
    if cbf then HTTPHandler[tag] = cbf end
end

-- 开始一个HTTP下载
-- @url         远程文件地址
-- @savePath    下载保存位置
-- @cbf         下载过程回调
function P.http_download(url, savePath, cbf)
    libnetwork.HttpDownload(url, savePath, CONNECT_TIMEOUT)
    if cbf then DownloadCbf[url] = cbf end
end

-- ============================================================================
-- 建立连接
function P.connect(host, port, onConnected, onInterrupt, onBroken)
    MainCli:set_connected(onConnected)
    MainCli:set_event(onInterrupt, onBroken)
    if MainCli:connected() then
        onConnected()
    else
        MainCli:connect(host, port)
    end
end

-- 客户端断开连接
function P.disconnect(name, keepHost)
    if name then
        local cli = ClientDEF.find(name)
        if cli then cli:disconnect(keepHost) end
    else
        GameCli:disconnect(keepHost)
        if GameCli ~= MainCli then MainCli:disconnect(keepHost) end
    end
end

-- 执行某个操作前检查是否wifi连接
function P.check_internet(action)
    local network = UE.Application.internetReachability
    if network == "ReachableViaLocalAreaNetwork" then
        if action then action() end
    else
        -- 非wifi网络
        UI.MBox.make()
            :set_param("content", _G.TEXT.tipAskUpdateViaCarrierDataNetwork)
            :set_event(action)
            :show()
    end
end

-- ============================================================================
-- 创建一个消息对象
P.msg = _G.DEF.Client.msg
P.gamemsg = _G.DEF.Client.gamemsg

-- 客户端发送消息
function P.send(nm, msg)
    GameCli:send(nm, msg)
end

-- 获取网络错误描述
function P.get_error(code, op)
    if op == nil then op = "Default" end
    local ret = config("errorlib").get_dat(op, code) or _G.TEXT.fmtUnknownError:csfmt(code)
    libunity.LogW("ERROR#{0}:{1}", code, ret)
    return ret
end

function P.chk_op_ret(ret, silent)
    local err = ret ~= 1 and P.get_error(ret) or nil
    if not silent then
        if err then
            err = cfgname({ name = err, id = ret,})
            _G.UI.Toast.make(nil, err):show()
        end
    end
    return ret, err
end

function P.common_op_ret(nm, silent)
    local ret, err = P.chk_op_ret(nm:readU32(), silent)
    return { ret = ret, err = err }
end

function P.empty_msg(nm) end

-- 用于判断是否使用本地数据中
function P.connected()
    local Player = rawget(DY_DATA, "Player")
    return Player ~= nil and Player.id ~= 0
end

-- ============================================================================
-- 注册消息分析器
-- 一个消息只能注册一次
function P.regist(code, handler)
    GameCli.regist_global(code, handler)
end

-- 订阅消息
function P.subscribe(code, handler)
    GameCli.subscribe_global(code, handler)
end

-- 取消订阅
function P.unsubscribe(code, handler)
    GameCli.unsubscribe_global(code, handler)
end

-- 发布消息
function P.broadcast(code, Ret)
    GameCli:broadcast(code, Ret)
end

return P