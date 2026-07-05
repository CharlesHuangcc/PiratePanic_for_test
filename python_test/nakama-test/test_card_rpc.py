import json

def test_rpc_load_user_cards(nakama_client):
    res = nakama_client.rpc_call("load_user_cards")
    data = json.loads(res["payload"])
    assert "deckCards" in data
    assert "storedCards" in data

def test_rpc_swap_deck_card(nakama_client):
    # 获取卡组
    card_raw = nakama_client.rpc_call("load_user_cards")["payload"]
    cards = json.loads(card_raw)
    deck_ids = list(cards["deckCards"].keys())
    store_ids = list(cards["storedCards"].keys())
    out_id = deck_ids[0]
    in_id = store_ids[0]
    # 执行换卡
    payload = {"cardOutId": out_id, "cardInId": in_id}
    nakama_client.rpc_call("swap_deck_card", payload)
    # 校验交换结果
    new_cards = json.loads(nakama_client.rpc_call("load_user_cards")["payload"])
    assert in_id in new_cards["deckCards"]
    assert out_id in new_cards["storedCards"]

def test_rpc_upgrade_card(nakama_client):
    cards = json.loads(nakama_client.rpc_call("load_user_cards")["payload"])
    first_id = list(cards["deckCards"].keys())[0]
    old_level = cards["deckCards"][first_id]["level"]

    nakama_client.rpc_call("upgrade_card", {"id": first_id}) # 升级卡牌

    new_cards = json.loads(nakama_client.rpc_call("load_user_cards")["payload"])

    assert new_cards["deckCards"][first_id]["level"] == old_level + 1

def test_rpc_reset_card_collection(nakama_client):
    # 先抽一张卡改变数据
    nakama_client.rpc_call("add_random_card")
    # 重置卡组
    nakama_client.rpc_call("reset_card_collection")
    reset_data = json.loads(nakama_client.rpc_call("load_user_cards")["payload"])
    assert len(reset_data["deckCards"]) == 6
    assert len(reset_data["storedCards"]) == 4