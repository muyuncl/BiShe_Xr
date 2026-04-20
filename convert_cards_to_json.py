#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
卡片数据转换脚本

Excel 列说明：
  ID / 卡片名称 / 描述 / 图片文件名 / 卡片组类型 / 政治目标 / 对应国家 / 禁忌国家 / 禁忌原因

卡片组类型：
  giftCards             - 国礼卡组（可填禁忌国家）
  cultureElements       - 传统文化元素卡组（可填禁忌国家）
  targetCountryElements - 目标国家文化元素（必填"对应国家"）
  taboos                - 本身就是某国禁忌元素（必填"对应国家"）

禁忌逻辑由 Unity 运行时处理：
  giftCards/cultureElements 填了禁忌国家 → 用户选该国时移入禁忌组
  taboos 类型 → 用户选对应国家时显示在禁忌组
"""

import pandas as pd
import json
import os
import shutil

# 自动切换到脚本所在目录
os.chdir(os.path.dirname(os.path.abspath(__file__)))

EXCEL_FILE = "cards.xlsx"
SOURCE_IMAGES_DIR = "Assets/Card photos"
OUTPUT_JSON_DIR = "Assets/Resources/CardData"
OUTPUT_IMAGES_DIR = "Assets/Resources/Images"
OUTPUT_JSON_FILE = os.path.join(OUTPUT_JSON_DIR, "cards.json")
VALID_TYPES = ["giftCards", "cultureElements", "targetCountryElements", "taboos"]


def create_directories():
    os.makedirs(OUTPUT_JSON_DIR, exist_ok=True)
    os.makedirs(OUTPUT_IMAGES_DIR, exist_ok=True)
    print("OK: 创建输出目录完成")


def read_excel():
    if not os.path.exists(EXCEL_FILE):
        print("ERR: 找不到文件 " + EXCEL_FILE)
        return None
    try:
        df = pd.read_excel(EXCEL_FILE)
        print("OK: 读取 Excel 成功，共 {} 行".format(len(df)))
        return df
    except Exception as e:
        print("ERR: 读取失败: {}".format(e))
        return None


def parse_list(value):
    if pd.isna(value):
        return []
    return [x.strip() for x in str(value).split(',') if x.strip()]


def get_str(row, col):
    val = row.get(col)
    if val is None:
        return ''
    try:
        if pd.isna(val):
            return ''
    except Exception:
        pass
    return str(val).strip()


def build_card(row):
    taboo_countries = parse_list(row.get('禁忌国家', ''))
    taboo_reason = get_str(row, '禁忌原因')
    return {
        "id": get_str(row, 'ID'),
        "name": get_str(row, '卡片名称'),
        "description": get_str(row, '描述'),
        "image": "Images/" + get_str(row, '图片文件名'),
        "politicalGoals": parse_list(row.get('政治目标', '')),
        "targetCountry": get_str(row, '对应国家'),
        "taboos": [{"country": c, "reason": taboo_reason} for c in taboo_countries]
    }


def convert_to_json(df):
    cards_data = {
        "giftCards": [],
        "cultureElements": [],
        "targetCountryElements": [],
        "taboos": []
    }

    for _, row in df.iterrows():
        card = build_card(row)
        card_type = get_str(row, '卡片组类型')

        if card_type not in VALID_TYPES:
            print("WARN: 未知类型 '{}' ({}), 跳过".format(card_type, card['name']))
            continue

        cards_data[card_type].append(card)

        if card_type == 'targetCountryElements':
            if card['targetCountry']:
                print("  >> '{}' -> 目标国家卡组({})".format(card['name'], card['targetCountry']))
            else:
                print("  WARN: '{}' 是 targetCountryElements 但未填'对应国家'!".format(card['name']))
        elif card_type == 'taboos':
            if card['targetCountry']:
                print("  >> '{}' -> 禁忌元素({})".format(card['name'], card['targetCountry']))
            else:
                print("  WARN: '{}' 是 taboos 但未填'对应国家'!".format(card['name']))
        elif card['taboos']:
            countries = [t['country'] for t in card['taboos']]
            print("  >> '{}' -> {}，在 {} 有禁忌".format(card['name'], card_type, countries))

    return cards_data


def copy_images(df):
    if not os.path.exists(SOURCE_IMAGES_DIR):
        print("ERR: 找不到图片文件夹 " + SOURCE_IMAGES_DIR)
        return
    ok, fail = 0, 0
    for _, row in df.iterrows():
        fn = get_str(row, '图片文件名')
        if not fn:
            continue
        src = os.path.join(SOURCE_IMAGES_DIR, fn)
        dst = os.path.join(OUTPUT_IMAGES_DIR, fn)
        if not os.path.exists(src):
            print("  WARN: 找不到图片: " + src)
            fail += 1
            continue
        try:
            shutil.copy2(src, dst)
            ok += 1
        except Exception as e:
            print("  ERR: 复制失败 {}: {}".format(fn, e))
            fail += 1
    print("OK: 图片复制完成：成功 {} 张，失败 {} 张".format(ok, fail))


def save_json(cards_data):
    try:
        with open(OUTPUT_JSON_FILE, 'w', encoding='utf-8') as f:
            json.dump(cards_data, f, ensure_ascii=False, indent=2)
        print("OK: JSON 保存成功: " + OUTPUT_JSON_FILE)
        return True
    except Exception as e:
        print("ERR: 保存失败: {}".format(e))
        return False


def print_summary(cards_data):
    print("\n" + "="*40)
    print("转换摘要")
    print("="*40)
    print("国礼卡组:             {} 张".format(len(cards_data['giftCards'])))
    print("传统文化元素卡组:     {} 张".format(len(cards_data['cultureElements'])))
    print("目标国家文化元素卡组: {} 张".format(len(cards_data['targetCountryElements'])))
    print("禁忌元素卡组:         {} 张".format(len(cards_data['taboos'])))
    print("总计: {} 张".format(sum(len(v) for v in cards_data.values())))
    print("="*40 + "\n")


def main():
    print("开始转换卡片数据...\n")
    create_directories()

    df = read_excel()
    if df is None:
        return False

    print("\n正在转换数据...")
    cards_data = convert_to_json(df)

    print("\n正在复制图片...")
    copy_images(df)

    print("\n正在保存 JSON...")
    if not save_json(cards_data):
        return False

    print_summary(cards_data)
    print("转换完成！")
    return True


if __name__ == "__main__":
    try:
        success = main()
        exit(0 if success else 1)
    except Exception as e:
        print("\nERR: 发生错误: {}".format(e))
        import traceback
        traceback.print_exc()
        exit(1)
