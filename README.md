# jsondo

一个基于JSON格式的文件操作工具，用于安全、高效地执行文件内容替换操作。

## 功能特点

- ✅ **JSON格式命令**：使用JSON文件定义替换操作，结构清晰易读
- ✅ **精确匹配**：支持字符串级别的精确匹配，包括空格、TAB和换行符
- ✅ **批量执行**：支持一次性执行多个命令文件
- ✅ **容错机制**：提供前向/后向扫描功能，提高查找成功率
- ✅ **自动备份**：执行前自动备份原文件和命令文件
- ✅ **特殊文件支持**：支持JavaScript/TypeScript模板字符串的转义换行符处理

## 安装

### 从源码编译

```bash
git clone <repository_url>
cd jsondo
make
sudo make install
```

### 卸载

```bash
sudo make uninstall
```

## 使用方法

### 基本语法

```bash
jsondo -f <command.json>
```

批量执行多个命令文件：

```bash
jsondo -f command1.json command2.json command3.json ...
```

### 查看帮助

```bash
jsondo -h
```

## 命令格式

### 1. replace_by_content - 替换文件中的指定文本

用于替换文件中的特定内容，适用于函数重写、文本修改等场景。

```json
{
  "commands": [
    {
      "call": "replace_by_content",
      "title": "替换文件中的旧文本",
      "args": {
        "file": "path/to/file.txt",
        "old_str": "旧文本内容",
        "new_str": "新文本内容",
        "startLine": 5,
        "backward_scan_limit": 10,
        "forward_scan_limit": 15
      }
    }
  ]
}
```

**参数说明：**

- `call`（必须）：命令类型，值为 `"replace_by_content"`
- `title`（可选）：命令标题，用于在控制台中区分不同命令
- `file`（必须）：目标文件路径
- `old_str`（必须）：要替换的旧文本内容
- `new_str`（必须）：新的文本内容
- `startLine`（可选，默认0）：开始搜索的行号（从1开始）
- `backward_scan_limit`（可选，默认10）：向前扫描的行数
- `forward_scan_limit`（可选，默认15）：向后扫描的行数

### 2. replace_by_range - 按行号范围替换内容

适用于需要替换大量内容时，通过开始和结束标记确定替换区间。

```json
{
  "commands": [
    {
      "call": "replace_by_range",
      "title": "替换函数实现",
      "args": {
        "file": "path/to/file.txt",
        "startLine": 5,
        "endLine": 10,
        "startLine_str": "起始验证文本",
        "endLine_str": "结束验证文本",
        "new_str": "新的多行内容",
        "backward_scan_limit": 10,
        "forward_scan_limit": 15
      }
    }
  ]
}
```

**参数说明：**

- `call`（必须）：命令类型，值为 `"replace_by_range"`
- `title`（可选）：命令标题
- `file`（必须）：目标文件路径
- `startLine`（必须）：开始行号（从1开始）
- `endLine`（必须）：结束行号（从1开始），设为 `-1` 表示替换到文件末尾
- `startLine_str`（必须）：起始位置的验证文本，用于确保替换位置正确
- `endLine_str`（必须）：结束位置的验证文本，用于确保替换位置正确
- `new_str`（必须）：新的多行内容
- `backward_scan_limit`（可选，默认10）：向前扫描的行数
- `forward_scan_limit`（可选，默认15）：向后扫描的行数

## 使用示例

### 示例1：简单文本替换

```json
{
  "commands": [
    {
      "call": "replace_by_content",
      "title": "修改配置文件",
      "args": {
        "file": "config.json",
        "old_str": "\"port\": 3000",
        "new_str": "\"port\": 8080"
      }
    }
  ]
}
```

### 示例2：多行代码替换

```json
{
  "commands": [
    {
      "call": "replace_by_range",
      "title": "重写calculate函数",
      "args": {
        "file": "src/math.js",
        "startLine": 10,
        "endLine": 20,
        "startLine_str": "function calculate(a, b) {",
        "endLine_str": "}",
        "new_str": "function calculate(a, b) {\n  return a * b + 1;\n}"
      }
    }
  ]
}
```

### 示例3：批量执行多个命令

命令文件 `fix_issues.json`：

```json
{
  "commands": [
    {
      "call": "replace_by_content",
      "title": "修复Bug #1",
      "args": {
        "file": "src/main.js",
        "old_str": "const x = 1",
        "new_str": "const x = 2"
      }
    },
    {
      "call": "replace_by_content",
      "title": "修复Bug #2",
      "args": {
        "file": "src/utils.js",
        "old_str": "return false",
        "new_str": "return true"
      }
    }
  ]
}
```

执行：

```bash
jsondo -f fix_issues.json
```

输出：

```
Eval command `修复Bug #1` from fix_issues.json
=== 修复Bug #1 ===
Replaced at line 15, deleted 1 lines, inserted 1 lines
=== 修复Bug #2 ===
Replaced at line 23, deleted 1 lines, inserted 1 lines
[OK] All changes from fix_issues.json[deleted] are applied.
```

## 工作原理

### 扫描策略

当直接匹配失败时，会使用前向/后向扫描机制：

1. **开始位置扫描**：从 `startLine - backward_scan_limit` 到 `startLine + forward_scan_limit` 范围内搜索
2. **结束位置扫描**：从 `endLine - backward_scan_limit` 到 `endLine + forward_scan_limit` 范围内搜索
3. **扫描方向**：
   - `backward`：逆向扫描，从指定行向前查找
   - `forward`：正向扫描，从指定行向后查找

### 自动备份

jsondo 会自动执行以下备份操作：

1. **命令文件备份**：成功执行后将命令文件备份到 `.jsondo/jsondo.lastApplied`
2. **原文件备份**：修改前会将原文件备份到 `.jsondo/jsondo.lastbackup`

### 特殊处理

- **换行符规范化**：自动将 Windows 风格换行符（`\r\n`）转换为 Unix 风格（`\n`）
- **JS/TS/TSX 模板字符串支持**：对包含反引号的文件，支持识别转义的换行符（`\n`、`\r\n`）进行正确的行分割

## 注意事项

- 文本匹配必须完全一致，包括所有空格、TAB符号、标点符号和转义字符
- 增加 `backward_scan_limit` 和 `forward_scan_limit` 可以提高查找成功率，但可能导致替换错误
- 建议先在备份文件上测试，确认无误后再应用到生产环境
- 批量执行时，如果其中一个命令失败，后续命令将不会执行

## 依赖

- cJSON：用于JSON解析
- GCC：编译器

## 许可证

详见 [LICENSE.txt](LICENSE.txt)

## 贡献

欢迎提交 Issue 和 Pull Request！
