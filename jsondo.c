#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <sys/stat.h>
#include <unistd.h>
#include "cJSON.h"

#define MAX_PATH_LEN 1024
#define MAX_STR_LEN 65536
#define MAX_LINES 10000
#define MAX_LINE_LEN 4096
#define MAX_COMMANDS 100
#define MAX_SEARCH_MARGIN 50

// 结构体定义
typedef struct {
    char call[64];
    char* args;  // JSON字符串格式的参数
} Command;

typedef struct {
    char file[MAX_PATH_LEN];
    char old_str[MAX_STR_LEN];
    char new_str[MAX_STR_LEN];
    int startLine;
    int backward_scan_limit;
    int forward_scan_limit;
} ReplaceByContentArgs;

typedef struct {
    char file[MAX_PATH_LEN];
    int startLine;
    int endLine;
    char new_str[MAX_STR_LEN];
    char startLine_str[MAX_STR_LEN];
    char endLine_str[MAX_STR_LEN];
    int backward_scan_limit;
    int forward_scan_limit;
} ReplaceByLinesArgs;

// 函数声明
void print_help();
int parse_json_file(const char* filename, Command commands[], int* command_count);
int eval_command(const char* json_content, const char* command_file);
int execute_replace_by_content(cJSON* args_json);
int execute_replace_by_range(cJSON* args_json);
int replace_by_content(const char* file_path, const char* old_str, int start_line, const char* new_str, 
                      int backward_scan_limit, int forward_scan_limit);
int replace_by_range(const char* file_path, int start_line, int end_line, const char* new_str, 
                     const char* start_line_str, const char* end_line_str, 
                     int backward_scan_limit, int forward_scan_limit);
void delete_command_file(const char* command_file);
int copy_file(const char* src_path, const char* dst_path);
char* trim(char* str);
char* str_replace(const char* str, const char* old, const char* new);
char* read_file(const char* filename);
int write_file(const char* filename, const char* content);
int file_exists(const char* filename);
int index_to_line(const char* str, int index);
int find_first_line(char* lines[], int line_count, int start_index, const char* search);
int find_last_line(char* lines[], int line_count, int start_index, const char* search);
int is_line_text_equal(const char* line1, const char* line2, int line_number);
int replace_line_by_line(const char* file_path, char* content_lines[], int line_count,
                         int start_line, char* search_lines[], int search_count,
                         char* insert_lines[], int insert_count,
                         int backward_scan_limit, int forward_scan_limit);
char** split_lines(const char* str, int* count);
char** split_special_multiline(const char* file_path, const char* text, int* count);
int is_multi_lines_equal(char* lines[], int line_count, int start_index, const char* comparing_str);
int locate_multi_lines_forward(char* search_lines[], int search_count, 
                               char* source_lines[], int source_count, 
                               int source_start, int forward);
int locate_multi_lines_backward(char* search_lines[], int search_count, 
                                char* source_lines[], int source_count, 
                                int source_start, int backward);
void free_string_array(char** array, int count);
char* get_file_extension(const char* file_path);
int is_special_extension(const char* ext);

// 主函数
int main(int argc, char* argv[]) {
    // 创建目录
    mkdir(".jsondo", 0755);
    
    // 显示帮助信息
    if (argc == 1 || strcmp(argv[1], "/help") == 0 || strcmp(argv[1], "-h") == 0) {
        print_help();
        return 0;
    }
    
    // 解析 -f 参数并执行命令文件
    if (argc >= 3 && strcmp(argv[1], "-f") == 0) {
        int all_success = 1;

        // 遍历所有命令文件
        for (int i = 2; i < argc; i++) {
            const char* command_file = argv[i];
            if (!file_exists(command_file)) {
                printf("Command file not found: %s\n", command_file);
                all_success = 0;
                continue;
            }

            char* json_content = read_file(command_file);
            if (json_content == NULL) {
                printf("Failed to read command file: %s\n", command_file);
                all_success = 0;
                continue;
            }

            printf("Eval command from %s\n", command_file);

            int result = eval_command(json_content, command_file);
            free(json_content);

            if (result != 0) {
                all_success = 0;
            }

            printf("\n");
        }

        return all_success ? 0 : 1;
    } else {
        printf("Invalid arguments. Use -f <command_file> to specify the command file.\n");
        return 1;
    }
    
    return 0;
}

// 打印帮助信息
void print_help() {
    printf("Usage: jsondo -f <command_file>\n");
    printf("The command file should contain JSON instructions for the tool to execute. For example:\n");
    printf("{\n");
    printf("  \"commands\": [\n");
    printf("    {\n");
    printf("      \"call\": \"replace_by_content\",\n");
    printf("      \"args\": {\n");
    printf("        \"file\": \"path/to/file.txt\",\n");
    printf("        \"old_str\": \"old text\",\n");
    printf("        \"new_str\": \"new text\"\n");
    printf("      }\n");
    printf("    }\n");
    printf("  ]\n");
    printf("}\n");
    printf("\n");
    printf("Available commands:\n");
    printf("1. replace_by_content: Replace specific text in a file\n");
    printf("   Example JSON structure:\n");
    printf("   {\n");
    printf("     \"commands\": [\n");
    printf("       {\n");
    printf("         \"call\": \"replace_by_content\",\n");
    printf("         \"args\": {\n");
    printf("           \"file\": \"C:\\path\\to\\file.txt\",\n");
    printf("           \"old_str\": \"old text\",\n");
    printf("           \"new_str\": \"new text\"\n");
    printf("         }\n");
    printf("       }\n");
    printf("     ]\n");
    printf("   }\n");
    printf("\n");
    printf("2. replace_by_range: Replace content by line numbers with validation\n");
    printf("   Example JSON structure:\n");
    printf("   {\n");
    printf("     \"commands\": [\n");
    printf("       {\n");
    printf("         \"call\": \"replace_by_range\",\n");
    printf("         \"args\": {\n");
    printf("           \"file\": \"C:\\path\\to\\file.txt\",\n");
    printf("           \"startLine\": 5,\n");
    printf("           \"endLine\": 10,\n");
    printf("           \"startLine_str\": \"start line validation text\",\n");
    printf("           \"endLine_str\": \"end line validation text\",\n");
    printf("           \"new_str\": \"new multi-line content\"\n");
    printf("         }\n");
    printf("       }\n");
    printf("     ]\n");
    printf("   }\n");
    printf("\n");
}

// 解析并执行JSON命令
int eval_command(const char* json_content, const char* command_file) {
    cJSON* root = cJSON_Parse(json_content);
    if (root == NULL) {
        printf("Invalid JSON format\n");
        return 1;
    }
    
    cJSON* commands_array = cJSON_GetObjectItem(root, "commands");
    if (commands_array == NULL || !cJSON_IsArray(commands_array)) {
        printf("Invalid JSON format: missing 'commands' array\n");
        cJSON_Delete(root);
        return 1;
    }
    
    int success = 1;
    int command_count = cJSON_GetArraySize(commands_array);
    
    for (int i = 0; i < command_count; i++) {
        cJSON* command = cJSON_GetArrayItem(commands_array, i);
        if (command == NULL || !cJSON_IsObject(command)) {
            printf("Invalid command at index %d\n", i);
            success = 0;
            break;
        }
        
        cJSON* call_item = cJSON_GetObjectItem(command, "call");
        if (call_item == NULL || !cJSON_IsString(call_item)) {
            printf("Invalid JSON format: missing or invalid 'call' property\n");
            success = 0;
            break;
        }
        
        const char* tool_name = call_item->valuestring;
        
        cJSON* args_item = cJSON_GetObjectItem(command, "args");
        if (args_item == NULL || !cJSON_IsObject(args_item)) {
            printf("Invalid JSON format: missing or invalid 'args' object\n");
            success = 0;
            break;
        }

        // 获取可选的title字段
        cJSON* title_item = cJSON_GetObjectItem(command, "title");
        const char* title = (title_item != NULL && cJSON_IsString(title_item)) ? title_item->valuestring : NULL;

        // 显示当前命令
        if (title != NULL && strlen(title) > 0) {
            printf("  Executing: `%s`\n", title);
        }


        // 根据工具名称执行相应的操作（支持大小写不敏感）
        int operation_success = 0;
        char lower_tool_name[64] = {0};
        for (int j = 0; tool_name[j] && j < 63; j++) {
            lower_tool_name[j] = tolower(tool_name[j]);
        }

        if (strcmp(lower_tool_name, "replace_by_content") == 0) {
            operation_success = execute_replace_by_content(args_item);
        } else if (strcmp(lower_tool_name, "replace_by_range") == 0) {
            operation_success = execute_replace_by_range(args_item);
        } else {
            printf("Unsupported tool: %s\n", tool_name);
            operation_success = 0;
        }
        
        if (!operation_success) {
            success = 0;
            break;
        }
    }
    
    cJSON_Delete(root);
    
    // 只有在所有操作都成功时才删除命令文件
    if (success) {
        char backup_path[MAX_PATH_LEN];
        snprintf(backup_path, sizeof(backup_path), ".jsondo/jsondo.lastApplied");
        
        // 复制命令文件到备份位置
        copy_file(command_file, backup_path);
        
        delete_command_file(command_file);
    }
    
    return success ? 0 : 1;
}

// 删除命令文件
void delete_command_file(const char* command_file) {
    if (file_exists(command_file)) {
        remove(command_file);
        printf("[OK] All changes from %s[deleted] are applied.\n", command_file);
    }
}

// 复制文件（Linux兼容）
int copy_file(const char* src_path, const char* dst_path) {
    FILE* src = fopen(src_path, "rb");
    if (src == NULL) return 0;

    FILE* dst = fopen(dst_path, "wb");
    if (dst == NULL) {
        fclose(src);
        return 0;
    }

    char buffer[4096];
    size_t bytes;
    while ((bytes = fread(buffer, 1, sizeof(buffer), src)) > 0) {
        fwrite(buffer, 1, bytes, dst);
    }

    fclose(src);
    fclose(dst);
    return 1;
}

// 执行文件替换操作
int execute_replace_by_content(cJSON* args_json) {
    ReplaceByContentArgs args = {0};

    cJSON* file_item = cJSON_GetObjectItem(args_json, "file");
    if (file_item == NULL || !cJSON_IsString(file_item)) {
        printf("Missing or invalid file parameter\n");
        return 0;
    }
    char* temp_file = strdup(file_item->valuestring);
    char* file_trimmed = trim(temp_file);
    strncpy(args.file, file_trimmed, sizeof(args.file) - 1);
    free(temp_file);

    cJSON* old_str_item = cJSON_GetObjectItem(args_json, "old_str");
    if (old_str_item == NULL || !cJSON_IsString(old_str_item)) {
        printf("Missing or invalid old_str parameter\n");
        return 0;
    }
    strncpy(args.old_str, old_str_item->valuestring, sizeof(args.old_str) - 1);

    cJSON* new_str_item = cJSON_GetObjectItem(args_json, "new_str");
    if (new_str_item == NULL || !cJSON_IsString(new_str_item)) {
        printf("Missing or invalid new_str parameter\n");
        return 0;
    }
    strncpy(args.new_str, new_str_item->valuestring, sizeof(args.new_str) - 1);

    args.startLine = 0;
    cJSON* start_line_item = cJSON_GetObjectItem(args_json, "startLine");
    if (start_line_item != NULL && cJSON_IsNumber(start_line_item)) {
        args.startLine = start_line_item->valueint;
    }

    args.backward_scan_limit = 10;
    cJSON* backward_item = cJSON_GetObjectItem(args_json, "backward_scan_limit");
    if (backward_item != NULL && cJSON_IsNumber(backward_item)) {
        args.backward_scan_limit = backward_item->valueint;
    }

    args.forward_scan_limit = 15;
    cJSON* forward_item = cJSON_GetObjectItem(args_json, "forward_scan_limit");
    if (forward_item != NULL && cJSON_IsNumber(forward_item)) {
        args.forward_scan_limit = forward_item->valueint;
    }

    return replace_by_content(args.file, args.old_str, args.startLine, args.new_str,
                             args.backward_scan_limit, args.forward_scan_limit);
}

// 执行按行替换文件操作
int execute_replace_by_range(cJSON* args_json) {
    ReplaceByLinesArgs args = {0};

    cJSON* file_item = cJSON_GetObjectItem(args_json, "file");
    if (file_item == NULL || !cJSON_IsString(file_item)) {
        printf("Missing or invalid file parameter\n");
        return 0;
    }
    char* temp_file = strdup(file_item->valuestring);
    char* file_trimmed = trim(temp_file);
    strncpy(args.file, file_trimmed, sizeof(args.file) - 1);
    free(temp_file);

    cJSON* start_line_item = cJSON_GetObjectItem(args_json, "startLine");
    if (start_line_item == NULL || !cJSON_IsNumber(start_line_item)) {
        printf("Missing or invalid startLine parameter\n");
        return 0;
    }
    args.startLine = start_line_item->valueint;

    cJSON* end_line_item = cJSON_GetObjectItem(args_json, "endLine");
    if (end_line_item == NULL || !cJSON_IsNumber(end_line_item)) {
        printf("Missing or invalid endLine parameter\n");
        return 0;
    }
    args.endLine = end_line_item->valueint;

    cJSON* new_str_item = cJSON_GetObjectItem(args_json, "new_str");
    if (new_str_item == NULL || !cJSON_IsString(new_str_item)) {
        printf("Missing or invalid new_str parameter\n");
        return 0;
    }
    char* temp_new_str = strdup(new_str_item->valuestring);
    char* new_str_trimmed = trim(temp_new_str);
    strncpy(args.new_str, new_str_trimmed, sizeof(args.new_str) - 1);
    free(temp_new_str);

    cJSON* start_line_str_item = cJSON_GetObjectItem(args_json, "startLine_str");
    if (start_line_str_item == NULL || !cJSON_IsString(start_line_str_item)) {
        printf("Missing or invalid startLine_str parameter\n");
        return 0;
    }
    strncpy(args.startLine_str, start_line_str_item->valuestring, sizeof(args.startLine_str) - 1);

    cJSON* end_line_str_item = cJSON_GetObjectItem(args_json, "endLine_str");
    if (end_line_str_item == NULL || !cJSON_IsString(end_line_str_item)) {
        printf("Missing or invalid endLine_str parameter\n");
        return 0;
    }
    strncpy(args.endLine_str, end_line_str_item->valuestring, sizeof(args.endLine_str) - 1);

    args.backward_scan_limit = 10;
    cJSON* backward_item = cJSON_GetObjectItem(args_json, "backward_scan_limit");
    if (backward_item != NULL && cJSON_IsNumber(backward_item)) {
        args.backward_scan_limit = backward_item->valueint;
    }

    args.forward_scan_limit = 15;
    cJSON* forward_item = cJSON_GetObjectItem(args_json, "forward_scan_limit");
    if (forward_item != NULL && cJSON_IsNumber(forward_item)) {
        args.forward_scan_limit = forward_item->valueint;
    }

    return replace_by_range(args.file, args.startLine, args.endLine, args.new_str,
                           args.startLine_str, args.endLine_str,
                           args.backward_scan_limit, args.forward_scan_limit);
}

// 文件替换方法：将文件中的指定文本替换为新文本
int replace_by_content(const char* file_path, const char* old_str, int start_line, const char* new_str,
                      int backward_scan_limit, int forward_scan_limit) {
    if (!file_exists(file_path)) {
        printf("  File not found: %s\n", file_path);
        return 0;
    }
    
    char* content = read_file(file_path);
    if (content == NULL) {
        printf("  Failed to read file: %s\n", file_path);
        return 0;
    }
    
    // 规范化换行符为\n
    char* normalized_content = str_replace(content, "\r\n", "\n");
    char* normalized_old_str = str_replace(old_str, "\r\n", "\n");
    
    // 查找旧文本
    char* found = strstr(normalized_content, normalized_old_str);
    if (found == NULL) {
        // 尝试逐行替换
        int search_count = 0, insert_count = 0;
        char** search_lines = split_special_multiline(file_path, normalized_old_str, &search_count);
        char** insert_lines = split_special_multiline(file_path, new_str, &insert_count);

        // 分割内容为行
        int content_line_count = 0;
        char** content_lines = split_lines(normalized_content, &content_line_count);

        int result = replace_line_by_line(file_path, content_lines, content_line_count,
                                         start_line, search_lines, search_count,
                                         insert_lines, insert_count,
                                         backward_scan_limit, forward_scan_limit);

        // 释放内存
        free_string_array(search_lines, search_count);
        free_string_array(insert_lines, insert_count);
        free_string_array(content_lines, content_line_count);
        free(normalized_content);
        free(normalized_old_str);
        free(content);

        return result;
    }
    
    // 检查是否有多个匹配项
    char* next_found = strstr(found + strlen(normalized_old_str), normalized_old_str);
    if (next_found != NULL) {
        printf("  Multiple occurrences found: %s\n", file_path);
        free(normalized_content);
        free(normalized_old_str);
        free(content);
        return 0;
    }
    
    // 执行替换
    int index = found - normalized_content;
    char* new_content = str_replace(normalized_content, normalized_old_str, new_str);
    
    // 计算行数和删除/插入的行数
    int line_number = index_to_line(normalized_content, index);
    int old_line_count = 0, new_line_count = 0;
    char** old_lines = split_lines(normalized_old_str, &old_line_count);
    char** new_lines = split_lines(new_str, &new_line_count);
    
    // 备份原文件
    char backup_path[MAX_PATH_LEN];
    snprintf(backup_path, sizeof(backup_path), ".jsondo/jsondo.lastbackup");
    copy_file(file_path, backup_path);
    
    // 写入新内容
    if (write_file(file_path, new_content)) {
        printf("  Replaced at line %d, deleted %d lines, inserted %d lines\n",
               line_number, old_line_count, new_line_count);
    } else {
        printf("  Failed to write file: %s\n", file_path);
        free_string_array(old_lines, old_line_count);
        free_string_array(new_lines, new_line_count);
        free(new_content);
        free(normalized_content);
        free(normalized_old_str);
        free(content);
        return 0;
    }
    
    // 释放内存
    free_string_array(old_lines, old_line_count);
    free_string_array(new_lines, new_line_count);
    free(new_content);
    free(normalized_content);
    free(normalized_old_str);
    free(content);
    
    return 1;
}

// 按行替换文件方法
int replace_by_range(const char* file_path, int start_line, int end_line, const char* new_str,
                     const char* start_line_str, const char* end_line_str,
                     int backward_scan_limit, int forward_scan_limit) {
    if (!file_exists(file_path)) {
        printf("  File not found: %s\n", file_path);
        return 0;
    }
    
    // 读取文件所有行
    FILE* file = fopen(file_path, "r");
    if (file == NULL) {
        printf("Failed to open file: %s\n", file_path);
        return 0;
    }
    
    char* lines[MAX_LINES];
    int line_count = 0;
    char buffer[MAX_LINE_LEN];
    
    while (line_count < MAX_LINES && fgets(buffer, sizeof(buffer), file) != NULL) {
        // 移除换行符
        buffer[strcspn(buffer, "\r\n")] = '\0';
        lines[line_count] = strdup(buffer);
        line_count++;
    }
    fclose(file);
    
    // 验证行号范围
    if (start_line > line_count) {
        printf("  Start line %d exceeds file length %d\n", start_line, line_count);
        for (int i = 0; i < line_count; i++) free(lines[i]);
        return 0;
    }
    
    // 如果endLine为-1，则替换到文件末尾
    int actual_end_line = (end_line == -1) ? line_count : end_line;
    if (actual_end_line > line_count) {
        printf("  End line %d exceeds file length %d\n", actual_end_line, line_count);
        for (int i = 0; i < line_count; i++) free(lines[i]);
        return 0;
    }
    
    // 校验起始行内容
    int actual_start_line = start_line;
    int start_line_count = 0;
    char** start_lines = split_lines(start_line_str, &start_line_count);
    
    if (!is_multi_lines_equal(lines, line_count, start_line - 1, start_line_str)) {
        // 尝试从前面搜索
        int marker_start = locate_multi_lines_backward(start_lines, start_line_count,
                                                      lines, line_count,
                                                      start_line - 1, backward_scan_limit);
        if (marker_start == -1) {
            // 向后搜索
            marker_start = locate_multi_lines_forward(start_lines, start_line_count,
                                                     lines, line_count,
                                                     start_line - 1, forward_scan_limit);
        }

        if (marker_start == -1) {
            printf("  W: Start marker not found near LN-%d (±%d lines). \n", start_line, backward_scan_limit + forward_scan_limit);
            printf("  REQEUSTED: '%s'\n", start_line_str);
            printf("  ACTRUALLY: '%s'\n", lines[start_line - 1]);
            free_string_array(start_lines, start_line_count);
            for (int i = 0; i < line_count; i++) free(lines[i]);
            return 0;
        }

        actual_start_line = marker_start + 1;
    }
    
    // 校验结束行内容
    int actual_end = actual_end_line;
    int end_line_count = 0;
    char** end_lines = split_lines(end_line_str, &end_line_count);
    
    if (!is_multi_lines_equal(lines, line_count, actual_end - end_line_count, end_line_str)) {
        int min_start_line_of_end_marker = actual_start_line + start_line_count;
        int marker_start = locate_multi_lines_forward(end_lines, end_line_count,
                                                     lines, line_count,
                                                     min_start_line_of_end_marker - 1, forward_scan_limit);

        if (marker_start == -1) {
            printf("  WARN: End marker not found within %d lines after LN-%d.\n", forward_scan_limit, actual_end_line);
            printf("  REQEUSTED: '%s'\n", end_line_str);
            printf("  ACTRUALLY: '%s'\n", lines[actual_end_line - 1]);
            free_string_array(start_lines, start_line_count);
            free_string_array(end_lines, end_line_count);
            for (int i = 0; i < line_count; i++) free(lines[i]);
            return 0;
        }

        actual_end = marker_start + 1 + end_line_count;
        printf("  INFO: Searching extended, found end marker at LN-%d instead of LN-%d\n",
               marker_start + 1, end_line);
    }
    
    // 构建新内容
    FILE* new_file = fopen(file_path, "w");
    if (new_file == NULL) {
        printf("  Failed to open file for writing: %s\n", file_path);
        free_string_array(start_lines, start_line_count);
        free_string_array(end_lines, end_line_count);
        for (int i = 0; i < line_count; i++) free(lines[i]);
        return 0;
    }
    
    // 写入开始行之前的内容
    for (int i = 0; i < actual_start_line - 1; i++) {
        fprintf(new_file, "%s\n", lines[i]);
    }
    
    // 写入新内容
    int new_str_count = 0;
    char** new_lines = split_lines(new_str, &new_str_count);
    for (int i = 0; i < new_str_count; i++) {
        fprintf(new_file, "%s\n", new_lines[i]);
    }
    
    // 写入结束行之后的内容
    for (int i = actual_end; i < line_count; i++) {
        fprintf(new_file, "%s\n", lines[i]);
    }
    
    fclose(new_file);

    // 备份原文件
    char backup_path[MAX_PATH_LEN];
    snprintf(backup_path, sizeof(backup_path), ".jsondo/jsondo.lastbackup");
    copy_file(file_path, backup_path);

    if (actual_start_line != start_line || actual_end != actual_end_line) {
        printf("  Replaced %d lines LN%d~%d (adjusted from requested LN%d~%d) in: %s\n",
               actual_end - actual_start_line, actual_start_line, actual_end,
               start_line, actual_end_line, file_path);
    } else {
        printf("  Replaced %d lines LN%d~%d successfully in: %s\n",
               end_line - start_line, start_line, end_line, file_path);
    }
    
    // 释放内存
    free_string_array(start_lines, start_line_count);
    free_string_array(end_lines, end_line_count);
    free_string_array(new_lines, new_str_count);
    for (int i = 0; i < line_count; i++) free(lines[i]);
    
    return 1;
}

// 辅助函数实现
// 获取文件扩展名
char* get_file_extension(const char* file_path) {
    const char* dot = strrchr(file_path, '.');
    if (dot == NULL || dot == file_path) return "";
    return (char*)dot + 1;
}

// 检查是否是特定扩展名（不区分大小写）
int is_special_extension(const char* ext) {
    char lower_ext[16] = {0};
    for (int i = 0; ext[i] && i < 15; i++) {
        lower_ext[i] = tolower(ext[i]);
    }
    return (strcmp(lower_ext, "js") == 0 ||
            strcmp(lower_ext, "ts") == 0 ||
            strcmp(lower_ext, "tsx") == 0);
}

char* trim(char* str) {
    if (str == NULL) return NULL;
    
    // 去除尾部空白
    char* end = str + strlen(str) - 1;
    while (end >= str && isspace(*end)) end--;
    *(end + 1) = '\0';
    
    // 去除头部空白
    while (isspace(*str)) str++;
    
    return str;
}

char* str_replace(const char* str, const char* old, const char* new) {
    if (str == NULL || old == NULL || new == NULL) return NULL;
    
    int old_len = strlen(old);
    int new_len = strlen(new);
    
    // 计算新字符串的长度
    int count = 0;
    const char* pos = str;
    while ((pos = strstr(pos, old)) != NULL) {
        count++;
        pos += old_len;
    }
    
    int new_size = strlen(str) + count * (new_len - old_len) + 1;
    char* result = (char*)malloc(new_size);
    if (result == NULL) return NULL;
    
    char* current = result;
    const char* start = str;
    while ((pos = strstr(start, old)) != NULL) {
        int segment_len = pos - start;
        memcpy(current, start, segment_len);
        current += segment_len;
        memcpy(current, new, new_len);
        current += new_len;
        start = pos + old_len;
    }
    
    strcpy(current, start);
    return result;
}

char* read_file(const char* filename) {
    FILE* file = fopen(filename, "rb");
    if (file == NULL) return NULL;
    
    fseek(file, 0, SEEK_END);
    long size = ftell(file);
    fseek(file, 0, SEEK_SET);
    
    char* content = (char*)malloc(size + 1);
    if (content == NULL) {
        fclose(file);
        return NULL;
    }
    
    fread(content, 1, size, file);
    content[size] = '\0';
    fclose(file);
    
    return content;
}

int write_file(const char* filename, const char* content) {
    FILE* file = fopen(filename, "wb");
    if (file == NULL) return 0;
    
    fwrite(content, 1, strlen(content), file);
    fclose(file);
    
    return 1;
}

int file_exists(const char* filename) {
    struct stat buffer;
    return (stat(filename, &buffer) == 0);
}

int index_to_line(const char* str, int index) {
    if (str == NULL || index < 0 || index > (int)strlen(str)) return 0;
    
    int line = 1;
    for (int i = 0; i < index; i++) {
        if (str[i] == '\n') line++;
    }
    
    return line;
}

int find_first_line(char* lines[], int line_count, int start_index, const char* search) {
    if (start_index >= line_count) return -1;
    
    for (int i = start_index; i < line_count && i < start_index + 10; i++) {
        char* trimmed_line = trim(strdup(lines[i]));
        char* trimmed_search = trim(strdup(search));
        int result = (strcmp(trimmed_line, trimmed_search) == 0);
        free(trimmed_line);
        free(trimmed_search);
        
        if (result) return i;
    }
    
    return -1;
}

int find_last_line(char* lines[], int line_count, int start_index, const char* search) {
    if (start_index >= line_count) return -1;
    
    for (int i = start_index; i < line_count && i < start_index + 30; i++) {
        // 去除尾部空白比较
        char line_copy[MAX_LINE_LEN];
        strcpy(line_copy, lines[i]);
        char* line_end = line_copy + strlen(line_copy) - 1;
        while (line_end >= line_copy && isspace(*line_end)) line_end--;
        *(line_end + 1) = '\0';
        
        char search_copy[MAX_LINE_LEN];
        strcpy(search_copy, search);
        char* search_end = search_copy + strlen(search_copy) - 1;
        while (search_end >= search_copy && isspace(*search_end)) search_end--;
        *(search_end + 1) = '\0';
        
        if (strcmp(line_copy, search_copy) == 0) return i;
    }
    
    return -1;
}

int is_line_text_equal(const char* line1, const char* line2, int line_number) {
    if (strcmp(line1, line2) == 0) return 1;
    
    if (line_number != -1) {
        printf("==== LN-%d: This Line is Not Equal ==== \n", line_number);
        printf("REQEUSTED: %s\n\n", line2);
        printf("ACTRUALLY: %s\n\n", line1);
    }
    
    return 0;
}

int replace_line_by_line(const char* file_path, char* content_lines[], int line_count,
                         int start_line, char* search_lines[], int search_count,
                         char* insert_lines[], int insert_count,
                         int backward_scan_limit, int forward_scan_limit) {
    // 检查第一行
    int search_start_line = start_line - backward_scan_limit;
    if (search_start_line < 0) search_start_line = 0;
    int search_end_line = start_line + forward_scan_limit;

    int start_row = -1;
    for (int i = search_start_line; i <= search_end_line && i < line_count; i++) {
        if (strcmp(content_lines[i], search_lines[0]) == 0) {
            start_row = i;
            break;
        }
    }

    if (start_row == -1) {
        printf("  E: First line mismatch near LN-%d (±%d lines)\n", start_line, backward_scan_limit + forward_scan_limit);
        return 0;
    }

    if (start_row + search_count > line_count) {
        printf("  E: Total lines of the searching content is more than the rest lines of source\n");
        printf("  Searching lines sum: %d, but %d lines from LN-%d to the source.\n",
               search_count, line_count - start_line, start_line);
        return 0;
    }

    if (search_count > 1) {
        // 检查最后一行
        int search_end_start = start_row + search_count;
        int search_end_limit = forward_scan_limit;
        int end = -1;
        for (int i = search_end_start; i < search_end_start + search_end_limit && i < line_count; i++) {
            if (strcmp(content_lines[i], search_lines[search_count - 1]) == 0) {
                end = i;
                break;
            }
        }

        if (end == -1) {
            printf("  Last line mismatch near LN-%d.\n", start_row + search_count);
            return 0;
        }

        // 检查其他行
        if (search_count > 2) {
            for (int i = 1; i < search_count - 1; i++) {
                if (!is_line_text_equal(content_lines[start_row + i], search_lines[i], -1)) {
                    printf("  Matched first %d lines, but mismatch at at LN-%d\n", i, start_row + i);
                    is_line_text_equal(content_lines[start_row + i], search_lines[i], start_row + i);
                    return 0;
                }
            }
        }
    }

    // 所有行匹配，执行替换
    FILE* file = fopen(file_path, "w");
    if (file == NULL) {
        printf("  Failed to open file for writing: %s\n", file_path);
        return 0;
    }

    // 写入开始行之前的内容
    for (int i = 0; i < start_row; i++) {
        fprintf(file, "%s\n", content_lines[i]);
    }

    // 写入插入内容
    for (int i = 0; i < insert_count; i++) {
        fprintf(file, "%s\n", insert_lines[i]);
    }

    // 写入剩余内容
    for (int i = start_row + search_count; i < line_count; i++) {
        fprintf(file, "%s\n", content_lines[i]);
    }

    fclose(file);
    return 1;
}

char** split_lines(const char* str, int* count) {
    if (str == NULL) {
        *count = 0;
        return NULL;
    }
    
    // 计算行数
    *count = 1;
    const char* p = str;
    while (*p) {
        if (*p == '\n') (*count)++;
        p++;
    }
    
    char** lines = (char**)malloc(*count * sizeof(char*));
    if (lines == NULL) {
        *count = 0;
        return NULL;
    }
    
    // 分割字符串
    char* copy = strdup(str);
    char* token = strtok(copy, "\n");
    int i = 0;
    
    while (token != NULL && i < *count) {
        lines[i] = strdup(token);
        token = strtok(NULL, "\n");
        i++;
    }
    
    *count = i;  // 实际行数
    free(copy);
    return lines;
}

char** split_special_multiline(const char* file_path, const char* text, int* count) {
    // 获取文件扩展名
    const char* ext = get_file_extension(file_path);

    // 如果是js/ts/tsx文件，且文本包含反引号，按转义换行符分割
    if (is_special_extension(ext)) {
        // 检查文本中是否包含反引号
        if (strchr(text, '`') != NULL) {
            // 先按 `\n` 分割，然后处理每个 `\r\n`
            int temp_count = 0;
            char** temp_lines = split_lines(text, &temp_count);

            // 计算实际行数（包含转义换行符）
            int actual_count = 0;
            for (int i = 0; i < temp_count; i++) {
                // 查找 \r\n 和 \n 的转义形式
                char* pos = temp_lines[i];
                int has_escaped = 0;
                while ((pos = strstr(pos, "\\r\\n")) != NULL) {
                    actual_count++;
                    pos += 4;
                    has_escaped = 1;
                }
                pos = temp_lines[i];
                while ((pos = strstr(pos, "\\n")) != NULL) {
                    actual_count++;
                    pos += 2;
                    has_escaped = 1;
                }
                if (!has_escaped) {
                    actual_count++;
                }
            }

            // 分配结果数组
            char** result = (char**)malloc((actual_count + 1) * sizeof(char*));
            int result_index = 0;

            for (int i = 0; i < temp_count; i++) {
                char* line = temp_lines[i];
                char* pos = line;
                char* last_pos = line;
                int found = 0;

                // 查找并分割转义换行符
                while ((pos = strstr(pos, "\\r\\n")) != NULL) {
                    int len = pos - last_pos;
                    char* part = (char*)malloc(len + 1);
                    strncpy(part, last_pos, len);
                    part[len] = '\0';
                    result[result_index++] = part;
                    pos += 4;
                    last_pos = pos;
                    found = 1;
                }

                pos = last_pos;
                while ((pos = strstr(pos, "\\n")) != NULL) {
                    int len = pos - last_pos;
                    char* part = (char*)malloc(len + 1);
                    strncpy(part, last_pos, len);
                    part[len] = '\0';
                    result[result_index++] = part;
                    pos += 2;
                    last_pos = pos;
                    found = 1;
                }

                if (!found) {
                    result[result_index++] = strdup(line);
                }
            }

            result[result_index] = NULL;

            // 释放临时数组
            for (int i = 0; i < temp_count; i++) {
                free(temp_lines[i]);
            }
            free(temp_lines);

            *count = result_index;
            return result;
        }
    }

    // 默认按换行符分割
    return split_lines(text, count);
}

int is_multi_lines_equal(char* lines[], int line_count, int start_index, const char* comparing_str) {
    if (start_index >= line_count) return 0;
    
    int compare_count = 0;
    char** compare_lines = split_lines(comparing_str, &compare_count);
    
    if (start_index + compare_count > line_count) {
        free_string_array(compare_lines, compare_count);
        return 0;
    }
    
    for (int i = 0; i < compare_count; i++) {
        if (strcmp(lines[start_index + i], compare_lines[i]) != 0) {
            free_string_array(compare_lines, compare_count);
            return 0;
        }
    }
    
    free_string_array(compare_lines, compare_count);
    return 1;
}

int locate_multi_lines_forward(char* search_lines[], int search_count, 
                               char* source_lines[], int source_count, 
                               int source_start, int forward) {
    if (source_start < 0) source_start = 0;
    
    int range_start = -1;
    int offset = 0;
    
    for (int i = source_start; i < source_count && i < source_start + forward; i++) {
        if (strcmp(source_lines[i], search_lines[offset]) != 0) {
            offset = 0;
            range_start = -1;
            continue;
        } else {
            offset++;
            if (range_start == -1) range_start = i;
        }
        
        if (offset == search_count) break;
    }
    
    return (offset == search_count) ? range_start : -1;
}

int locate_multi_lines_backward(char* search_lines[], int search_count, 
                                char* source_lines[], int source_count, 
                                int source_start, int backward) {
    int range_start = -1;
    int processed = 0;
    int from_line_index = (source_start - backward < 0) ? 0 : source_start - backward;
    
    for (int i = source_start; i >= from_line_index; i--) {
        if (strcmp(source_lines[i], search_lines[search_count - processed - 1]) != 0) {
            processed = 0;
            range_start = -1;
            continue;
        } else {
            processed++;
            range_start = i;
        }
        
        if (processed == search_count) break;
    }
    
    return (processed == search_count) ? range_start : -1;
}

void free_string_array(char** array, int count) {
    if (array == NULL) return;
    
    for (int i = 0; i < count; i++) {
        if (array[i] != NULL) free(array[i]);
    }
    
    free(array);
}