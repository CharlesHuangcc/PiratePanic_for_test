import json
import os
import uuid
import requests
import pytest
from dotenv import load_dotenv

load_dotenv()


class NakamaClient:
    """封装Nakama HTTP请求、登录、RPC、存储、钱包操作"""
    def __init__(self):
        self.base_url = os.getenv("NAKAMA_URL")
        self.api_key = os.getenv("NAKAMA_API_KEY")
        self.token = None
        self.user_id = None
        self.username = None

    def _headers(self):
        headers = {}
        if self.token:
            # 已登录业务接口：Bearer token + json
            headers["Content-Type"] = "application/json"
            headers["Authorization"] = f"Bearer {self.token}"
        else:
            # 登录接口：表单格式 + Basic API Key
            headers["Content-Type"] = "application/x-www-form-urlencoded"
            import base64
            basic_auth = base64.b64encode(f"{self.api_key}:".encode()).decode()
            headers["Authorization"] = f"Basic {basic_auth}"
        return headers

    def authenticate_device(self, device_id: str, username: str, create: bool = True):
        """设备登录，自动创建账号"""
        url = f"{self.base_url}/v2/account/authenticate/device"
        payload = {
            "id": device_id,
            "username": username,
            "create": create
        }
        resp = requests.post(url, json=payload, headers=self._headers())
        resp.raise_for_status()
        data = resp.json()
        self.token = data["token"]
        return data

    def rpc_call(self, rpc_id: str, payload: dict = None):
        """调用自定义RPC函数"""
        url = f"{self.base_url}/v2/rpc/{rpc_id}"

        inner  = json.dumps(payload if payload else {}) # 业务字典转为普通json字符串
        body_str = json.dumps(inner) # 再包一层双引号，整体变成字符串

        headers = self._headers()

        # print("URL:", url)
        # print("Headers:", headers)
        # print("Request Body(json):", body_str,type(body_str))
        resp = requests.post(url, data=body_str, headers=headers) # json=body，改为data=body，Nakama Go 后端期望 HTTP Body 是纯字符串
        # print("Status Code:", resp.status_code)
        # print("Raw Response:", resp.text)
        resp.raise_for_status()
        return resp.json()

    def create_group(self, group_name: str):
        """创建公会（clan）"""
        url = f"{self.base_url}/v2/groups"
        payload = {
            "name": group_name,
            "description": "test clan",
            "lang_tag": "en",
            "private": False
        }
        resp = requests.post(url, json=payload, headers=self._headers())
        resp.raise_for_status()
        return resp.json()

    def add_friend(self, target_user_id: str):
        """添加好友触发afterAddFriends钩子"""
        url = f"{self.base_url}/v2/friends"
        payload = {"ids": [target_user_id]}
        resp = requests.post(url, json=payload, headers=self._headers())
        resp.raise_for_status()
        return resp.json()


    def list_notifications(self):
        """拉取通知，校验公会/任务推送"""
        url = f"{self.base_url}/v2/notifications"
        resp = requests.get(url, headers=self._headers())
        resp.raise_for_status()
        return resp.json()

# Pytest 夹具：全新测试用户
@pytest.fixture(scope="function")
def nakama_client():
    client = NakamaClient()
    dev_id = str(uuid.uuid4())
    username = f"test_{uuid.uuid4().hex[:8]}"
    client.authenticate_device(dev_id, username)
    yield client

# # 夹具：创建第二个用户（用于好友、公会测试）
# @pytest.fixture(scope="function")
# def nakama_client_second():
#     client = NakamaClient()
#     dev_id = str(uuid.uuid4())
#     username = f"test2_{uuid.uuid4().hex[:8]}"
#     client.authenticate_device(dev_id, username)
#     yield client

# # 夹具：创建测试公会
# @pytest.fixture(scope="function")
# def test_clan(nakama_client):
#     group = nakama_client.create_group(f"TestClan_{uuid.uuid4().hex[:6]}")
#     return group
