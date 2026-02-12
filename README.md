# 个人学习unity开发的联机FPS游戏

## 本仓库包含内容

- `Scripts`: unity游戏开发过程中的所有c#脚本文件，包括玩家信息类，玩家控制类，联机网络类，武器管理类，UI显示类等。
- `Prefabs`: 开发中用到的预制体，涵盖下载的和自制的资源组件。
- `backend`: 使用django框架，开发后端管理系统，包括玩家房间的创建，房间查询，房间的删除.
- `Build`: 游戏打包后的可执行文件，包含`windows`版本的客户端和`linux`版本的服务端。
  - 服务端类似websocket服务器，负责处理玩家的联机请求和数据传输。
  - 玩家下载客户端进行游戏联机。

### 效果图
<img width="1776" height="1965" alt="image" src="https://github.com/user-attachments/assets/abce356a-befd-42ec-a7f1-85b351d20594" />


