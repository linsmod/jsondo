# Makefile for jsondo

# 变量定义
CC = gcc
CFLAGS = -I./cJSON -Wall -O2
LDFLAGS = -lm
TARGET = jsondo
SRC = jsondo.c
CJSON_SRC = cJSON/cJSON.c
CJSON_HDR = cJSON/cJSON.h

# 默认目标
all: $(TARGET)

# 编译目标
$(TARGET): $(SRC) $(CJSON_SRC) $(CJSON_HDR)
	$(CC) $(CFLAGS) -o $(TARGET) $(SRC) $(CJSON_SRC) $(LDFLAGS)
	@echo "Build complete: $(TARGET)"

# 清理
clean:
	rm -f $(TARGET)
	rm -f cJSON/*.o
	@echo "Clean complete"

# 安装
install: $(TARGET)
	@echo "Installing $(TARGET) to /usr/local/bin..."
	install -d /usr/local/bin
	install -m 755 $(TARGET) /usr/local/bin/
	@echo "Installation complete. You can now run 'jsondo' from anywhere."

# 卸载
uninstall:
	@echo "Uninstalling $(TARGET) from /usr/local/bin..."
	rm -f /usr/local/bin/$(TARGET)
	@echo "Uninstallation complete."

# 帮助信息
help:
	@echo "Available targets:"
	@echo "  all       - Build the jsondo program (default)"
	@echo "  clean     - Remove built files"
	@echo "  install   - Install jsondo to /usr/local/bin (requires sudo)"
	@echo "  uninstall - Remove jsondo from /usr/local/bin (requires sudo)"
	@echo "  help      - Show this help message"
	@echo ""
	@echo "Examples:"
	@echo "  make              - Build the program"
	@echo "  make install       - Install the program"
	@echo "  sudo make install - Install with sudo privileges"

# 伪目标
.PHONY: all clean install uninstall help
