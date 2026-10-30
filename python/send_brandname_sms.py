#!/usr/bin/env python3
# ============================================================================
# HỆ THỐNG QUẢN LÝ PHÒNG GAME/NET (NETCAFE MANAGEMENT SYSTEM)
# File: python/send_brandname_sms.py
# Mục đích: [CLI TOOL] Bắn SMS Brandname / Voucher khuyến mãi trực tiếp về điện thoại
# ============================================================================

import sys
import json
import argparse
import urllib.request

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

NTFY_TOPIC = "netcafe_vietanh"

def push_sms(phone, name, amount=None, balance=None, is_promo=False):
    # Đảm bảo an ninh tuyệt đối: Chỉ gửi đích danh cho Nguyễn Việt Anh (0327469105)
    if phone != "0327469105":
        print(f"[!] BẢO VỆ AN TOÀN: Hệ thống chỉ cấu hình gửi riêng cho Nguyễn Việt Anh (0327469105). Từ chối gửi tới {phone}.")
        return False

    if is_promo:
        orig_price = 10000
        discount_percent = 25
        discount_amount = int(orig_price * (discount_percent / 100))
        final_price = orig_price - discount_amount
        
        title = f"🎁 NetCafe: Giảm 25% Tiền Giờ (10k -> 7.5k) - {name}"
        msg = (
            f"[NETCAFE PRO] Chào bạn {name}! Khung giờ vàng kích cầu: "
            f"Giá gốc {orig_price:,}đ/h -> GIẢM {discount_percent}% (-{discount_amount:,}đ) "
            f"chỉ còn {final_price:,}đ/h! Tặng kèm mã VIP30K giảm 30.000đ. Hẹn gặp bạn tại NetCafe!"
        )
    else:
        amt_str = f"{amount:,} VND" if amount else "Giao dịch"
        bal_str = f"{balance:,} VND" if balance else "Số dư cập nhật"
        title = f"💳 NetCafe: Biến Động Số Dư (+{amt_str}) - {name}"
        msg = (
            f"[NETCAFE PRO] Khách hàng {name} vừa nạp thành công: +{amt_str}. "
            f"Số dư tài khoản hiện tại: {bal_str}. Chúc bạn chơi game vui vẻ!"
        )

    print("=" * 75)
    print("      NETCAFE BRANDNAME SMS & VOUCHER DISPATCHER (CLI TOOL)      ")
    print("=" * 75)
    print(f"[*] Số điện thoại nhận : {phone}")
    print(f"[*] Tên khách hàng     : {name}")
    if is_promo:
        print(f"[*] Chính sách giá     : Giá gốc 10.000đ/h -> GIẢM 25% (-2.500đ) -> Còn 7.500đ/h!")
        print(f"[*] Mã khuyến mãi AI   : HAPPY_AI_16H + VIP_VIETANH_30K")
    print(f"[*] Nội dung tin nhắn  : {msg}")
    print("-" * 75)

    try:
        url = "https://ntfy.sh/"
        payload = {
            "topic": NTFY_TOPIC,
            "title": title,
            "message": msg,
            "priority": 4,
            "tags": ["tada", "iphone", "partying_face"]
        }
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        req = urllib.request.Request(
            url,
            data=data,
            headers={"Content-Type": "application/json; charset=utf-8"}
        )
        with urllib.request.urlopen(req, timeout=5) as resp:
            if resp.status == 200:
                print(f"[📲 TRẠNG THÁI GỬI SMS]: THÀNH CÔNG (HTTP 200 OK - DELIVERED)")
                print(f"[✅] Đã gửi tin nhắn SMS thành công tới số điện thoại {phone}!")
                print(f"[🔗] Kiểm tra trực tiếp trên điện thoại tại: https://ntfy.sh/{NTFY_TOPIC}")
                print("=" * 75)
                return True
    except Exception as e:
        print(f"[!] Lỗi khi gửi SMS Gateway: {e}")
        return False

def main():
    parser = argparse.ArgumentParser(description="Gửi tin nhắn SMS Brandname mô phỏng")
    parser.add_argument("--phone", default="0327469105", help="Số điện thoại")
    parser.add_argument("--name", default="Nguyen Viet Anh", help="Tên khách hàng")
    parser.add_argument("--amount", type=int, default=None, help="Số tiền giao dịch")
    parser.add_argument("--balance", type=int, default=None, help="Số dư tài khoản")
    parser.add_argument("--promo", action="store_true", help="Gửi voucher khuyến mãi kích cầu")

    args = parser.parse_args()
    push_sms(
        phone=args.phone,
        name=args.name,
        amount=args.amount,
        balance=args.balance,
        is_promo=args.promo
    )

if __name__ == "__main__":
    main()

