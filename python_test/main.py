import requests, base64, uuid
API_KEY = "defaultkey"
url = "http://127.0.0.1:7350/v2/account/authenticate/device"
dev_id = str(uuid.uuid4())
# JSON body 字段必须是 id，不是 device_id
payload = {
    "id": dev_id,
    "username": "test_demo",
    "create": True
}
# Basic鉴权头
auth = base64.b64encode(f"{API_KEY}:".encode()).decode()
headers = {
    "Content-Type": "application/json",
    "Authorization": f"Basic {auth}"
}
resp = requests.post(url, json=payload, headers=headers)
print(resp.status_code, resp.text)