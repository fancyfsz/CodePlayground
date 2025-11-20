#!/bin/bash

# 用法: ./catalog_checker.sh Catalog_HS_Android_Debug_test_xxx.bin

if [ $# -lt 1 ]; then
    echo "Usage: $0 <catalog_file>"
    exit 1
fi

FILE="$1"

# 检查文件是否存在
if [ ! -f "$FILE" ]; then
    echo "File not found: $FILE"
    exit 1
fi

echo "Checking catalog file: $FILE"

# 读取前 12 字节，假设每个字段 4 字节
# 小端格式，输出为十进制
HEADER_SIZE=$(xxd -p -l 4 -s 0 "$FILE" | xxd -r -p | od -An -t u4)
ENTRIES=$(xxd -p -l 4 -s 4 "$FILE" | xxd -r -p | od -An -t u4)
PROVIDERS=$(xxd -p -l 4 -s 8 "$FILE" | xxd -r -p | od -An -t u4)

echo "Header size : $HEADER_SIZE"
echo "Entries     : $ENTRIES"
echo "Providers   : $PROVIDERS"
