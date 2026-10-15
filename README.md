# itch tools

itch.io 网页游戏的几个自用工具，Windows 上用。全部打包成免安装的单文件 exe，拷走就能用。

> 本仓库**只放工具，不含任何游戏文件、素材**。游戏版权归原作者所有，请自己从 itch.io 官网下载，工具仅用于你合法获取的内容（免费 / 公开 / 已购买）。

## 里面有什么

- `itchio-downloader/` — itch.io 游戏下载器（普通版 + MAX 智能版）
- `game-toolbox/` — 已下载游戏的启动器，支持本机玩和局域网手机玩
- `cloud-server/` — 把本地网页游戏临时开一个"链接 + 6 位访问码"的服务器，手机平板输地址就能玩

## 下载

编译好的 exe 在 [Releases](https://github.com/BSGGPRO/itchtools/releases) 页。
Windows 10 / 11（64 位），不需要装任何运行库。首次运行提示"未知发布者"就点"更多信息 → 仍要运行"。

## 许可证

- `itchio-downloader/`、`game-toolbox/` 基于 [Wal33D/itchio-downloader](https://github.com/Wal33D/itchio-downloader) 修改封装，原项目 ISC License，版权归 Wal33D，许可文本见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)
- `cloud-server/` 用到 Cloudflare 的 cloudflared（Apache-2.0）
- 其余自己写的代码 MIT，见 [LICENSE](LICENSE)

## 使用政策

只下载 / 游玩免费、公开或已购买的内容，不绕过付费限制，遵守 [itch.io ToS](https://itch.io/docs/legal/terms) 和各游戏作者的规则。
