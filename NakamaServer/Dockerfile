FROM node:alpine AS node-builder

WORKDIR /backend

# 缓存依赖层，不常变动
COPY package*.json ./
RUN npm install

# 一次性复制全部项目文件（tsconfig + 所有src），避免缓存跳过执行问题,需配合.dockerignore，排除多余文件
COPY . ./

# 容器内完整编译TS
RUN npx tsc

# Nakama运行镜像
FROM heroiclabs/nakama:3.39.0
COPY --from=node-builder /backend/build/*.js /nakama/data/modules/build/
COPY local.yml /nakama/data/