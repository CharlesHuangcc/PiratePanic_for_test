# PiratePanic_for_test
This is an automated testing practice project based on Nakama's sample project “Pirate Panic” (which includes both client and server components).

# 项目所需工具

- Docker29.5.2，用以管理jenkins、nakama、postgres的镜像
- Python3.12，额外需要uv管理工具
- JDK17，启动Jenkins 服务
- Jenkins，开源工具，用于自动化软件打包、测试、部署
- Unity2022.3.44f1c1，开发游戏客户端，额外需要Unity Test Framework
- nakama，一个开源的在线和多人服务器框架，作为游戏的服务器
- postgres，与nakama配套使用的数据库
- Node.js，nakama业务代码采用TypeScript，用于编译代码


# 服务器环境准备

## 前提条件
- 为TypeScript工具准备：Node v14 或更高
- 为Nakama服务器、PostgreSQL准备：Docker
	可参考 Heroic Labs[入门指南](https://heroiclabs.com/docs/nakama/getting-started/install/docker/)

## 准备TypeScript脚本环境
参考：[TypeScript Runtime](https://heroiclabs.com/docs/nakama/server-framework/typescript-runtime/)

```PowerShell
# 1输入
PS D:\codeTrain\PiratePanic_for_test\NakamaServer> npm init -y
# 1输出
Wrote to D:\codeTrain\PiratePanic_for_test\NakamaServer\package.json:

{
  "name": "nakamaserver",
  "version": "1.0.0",
  "description": "",
  "main": "index.js",
  "scripts": {
    "test": "echo \"Error: no test specified\" && exit 1"
  },
  "keywords": [],
  "author": "",
  "license": "ISC",
  "type": "commonjs"
}

# 2输入，这里指定typescript为4.2.4版本
PS D:\codeTrain\PiratePanic_for_test\NakamaServer> npm install --save-dev typescript@4.2.4
# 2输出
added 1 package, and audited 2 packages in 2s
found 0 vulnerabilities
# 3输入
PS D:\codeTrain\PiratePanic_for_test\NakamaServer> npx tsc --init
# 3输出
message TS6071: Successfully created a tsconfig.json file.
# 4输入
PS D:\codeTrain\PiratePanic_for_test\NakamaServer> npm i 'https://github.com/heroiclabs/nakama-common'
# 4输出，将 Nakama 运行时类型作为项目的依赖
npm warn gitignore-fallback No .npmignore file found, using .gitignore for file exclusion. Consider creating a .npmignore file to explicitly control published files.
added 1 package, and audited 3 packages in 20s
found 0 vulnerabilities
# 5输入，构建TS
PS D:\codeTrain\PiratePanic_for_test\NakamaServer> npx tsc
# 5输出
PS D:\codeTrain\PiratePanic_for_test\NakamaServer>

```

在3输入输出执行完毕后，需要修改`tsconfig.json`，并保存文件，再继续往下执行4、5部分：

- 屏蔽`"module": "commonjs",`
- 确认`"target": "es2016",`的值修改为`es5`
- `"outFile"`修改为`"outFile": "./build/index.js",`
- `"typeRoots"`修改为：`"typeRoots": ["./node_modules"],`

- 新建"files"，添加`/src`目录下的文件，准备编译

  ```json
  {
    "files": [
      "./src/clans.ts",
      "./src/deck.ts",
      "./src/economy.ts",
      "./src/main.ts",
      "./src/quests.ts",
      "./src/match.ts",
    ],
    "compilerOptions": {
      // ... etc
    }
  }
  ```


## 搭建 Docker

确认已安装Docker。

进入`.\NakamaServer`目录，确认有`docker-compose.yml`、`Dockerfile`、`local.yml`。

打开终端运行：`docker-compose up`，会等待下载`Image postgres:12.2`，构建`Image nakama-1`，完毕自动运行。

Docker环境准备好后，通过日志确认服务器、数据库已就绪。

```TEXT
...
postgres  | 2026-07-01 07:45:01.089 UTC [1] LOG:  database system is ready to accept connections
...
nakama-1  | {"level":"info","ts":"2026-07-01T07:45:03.584Z","caller":"main.go:243","msg":"Startup done"}
```



## 更新服务器脚本

当有新修改TypeScript文件，需要重新编译代码库，再重新运行，确保镜像能用你最新的更改重建。

```bash
npx tsc
docker compose up --build nakama
```

# 客户端环境准备

## 前提条件
- 需要 Unity 2020.3.7f1或更高版本，最好是 Unity 2022.3.44f1c1 。

## 设置、运行

（1）确认服务器启动成功。

（2）本项目在 Unity 2022.3.44f1c1 版本打开`./PiratePanic`的Unity项目。

（3）打开 Unity 控制台窗口（Unity 在 Windows→通用→控制台），确认没有任何警告或错误。

（4）默认情况下，游戏会尝试与 localhost 的 7350 端口通信，这是 Nakama 的默认 HTTP 端口。

（5）Scene搜索`Scene01MainMenu.unity`并打开，运行客户端

# 本项目自动化测试流程

1 git仓库代码修改，触发jenkins检查流水线

2.1 静态配置检查，采用Unity的UTF的EditMode检查unity内ScriptableObject类配置；或python检查excel、csv配置表等。通过率低则中断，返回警告。

2.2 客户端代码检查，采用Unity的UTF的EditMode检查C#代码，单元测试。通过率低则中断，返回警告。

2.3 服务端业务代码检查，利用python的pytest库，进行单元测试。通过率低则中断，返回警告。

3.1 编译客户端，写python脚本，构建unity。失败则中断，返回警告。

3.2 启动服务器，Docker重建，启动nakama+postgres。失败则中断，返回警告。

4.1 客户端集成测试，采用Unity的UTF的PlayMode，运行检查各模块Manager脚本。通过率低则中断，返回警告。

4.2 服务端协议测试，写python脚本（利用pytest），进行服务器、数据库接口联调、协议收发正确性测试。通过率低则中断，返回警告。

5.1 写python脚本（利用airtest，或python的opencv库的模板匹配），执行冒烟测试，运行、联机、战斗等主流程测试。通过率低则中断，返回警告。

5.2 进行回归测试，跑更多测试用例。通过率低则中断，返回警告。

6 返回测试报告。
