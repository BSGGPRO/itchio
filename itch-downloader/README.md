# itch-downloader

itch.io 游戏下载器，Windows 免安装单文件。基于 [Wal33D/itchio-downloader](https://github.com/Wal33D/itchio-downloader)（ISC License）改的，加了个网页操作界面、链接自动识别、Cookie 注入这些，用 PyInstaller 打包。

- `itch-downloader.exe` — 普通版
- `itch-downloader-max.exe` — MAX 智能版（带图形界面）

## 用法（MAX 版）

1. 双击 exe，浏览器自动打开操作界面（黑色窗口别关，关了就是退出）；
2. 把 itch.io 游戏页面链接粘到输入框 → 点【分析链接】；
3. 点【开始下载】，游戏会存到电脑的"下载"文件夹\itch-games（改过下载位置也能识别到）；
4. 下完用 game-toolbox 启动玩。

## 识别规则

- 网页游戏：抓全部网页资源，Unity 大游戏、子目录嵌入都支持；
- 文件包：直接下 zip/exe；
- 外部托管：作者服务器上的游戏，自动镜像下载；
- 付费 / 需登录游戏：免费下不了，已购买的可以浏览器 F12 复制 Cookie 粘到"高级：itch.io Cookie"再下。

## 小问题

- 没自动开浏览器：手动打开黑色窗口里显示的 `http://127.0.0.1:端口`；
- 提示 429：itch.io 限流，等几分钟（工具会自动重试 3 次）；
- 系统代理关了没关系，工具会自己探测常见代理端口（7897 等）。

## 使用政策

只下载免费 / 公开 / 已购买内容，不绕过付费限制，遵守 itch.io 和游戏作者的规则。
