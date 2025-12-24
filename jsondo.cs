using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace json_do
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 显示帮助信息
            if(args.Length == 0 || "/help" == args[0] || "-h"== args[0])
            {
                Console.WriteLine("Usage: jsondo -f <command_file>");
                Console.WriteLine("The command file should contain JSON instructions for the tool to execute. For example:");
                Console.WriteLine("{");
                Console.WriteLine("  \"commands\": [");
                Console.WriteLine("    {");
                Console.WriteLine("      \"call\": \"replace_by_content\",");
                Console.WriteLine("      \"args\": {");
                Console.WriteLine("        \"file\": \"path/to/file.txt\",");
                Console.WriteLine("        \"old_str\": \"old text\",");
                Console.WriteLine("        \"new_str\": \"new text\"");
                Console.WriteLine("      }");
                Console.WriteLine("    }");
                Console.WriteLine("  ]");
                Console.WriteLine("}");
                Console.WriteLine();
                Console.WriteLine("Available commands:");
                Console.WriteLine("1. replace_by_content: Replace specific text in a file");
                Console.WriteLine("   Example JSON structure:");
                Console.WriteLine("   {");
                Console.WriteLine("     \"commands\": [");
                Console.WriteLine("       {");
                Console.WriteLine("         \"call\": \"replace_by_content\",");
                Console.WriteLine("         \"args\": {");
                Console.WriteLine("           \"file\": \"C:\\path\\to\\file.txt\",");
                Console.WriteLine("           \"old_str\": \"old text\",");
                Console.WriteLine("           \"new_str\": \"new text\"");
                Console.WriteLine("         }");
                Console.WriteLine("       }");
                Console.WriteLine("     ]");
                Console.WriteLine("   }");
                Console.WriteLine();
                Console.WriteLine("2. replace_by_lines: Replace content by line numbers with validation");
                Console.WriteLine("   Example JSON structure:");
                Console.WriteLine("   {");
                Console.WriteLine("     \"commands\": [");
                Console.WriteLine("       {");
                Console.WriteLine("         \"call\": \"replace_by_lines\",");
                Console.WriteLine("         \"args\": {");
                Console.WriteLine("           \"file\": \"C:\\path\\to\\file.txt\",");
                Console.WriteLine("           \"startLine\": 5,");
                Console.WriteLine("           \"endLine\": 10,");
                Console.WriteLine("           \"startLine_str\": \"start line validation text\",");
                Console.WriteLine("           \"endLine_str\": \"end line validation text\",");
                Console.WriteLine("           \"new_str\": \"new multi-line content\"");
                Console.WriteLine("         }");
                Console.WriteLine("       }");
                Console.WriteLine("     ]");
                Console.WriteLine("   }");
                Console.WriteLine();
                return;
            }
            
            // 解析 -f 参数并执行命令文件
            if (args[0] == "-f" && args.Length > 1)
            {
                string commandFile = args[1];
                if (!File.Exists(commandFile))
                {
                    Console.WriteLine("Command file not found: " + commandFile);
                    return;
                }
                string jsonContent = System.IO.File.ReadAllText(commandFile);
                eval_command(jsonContent, commandFile);
            }
            else
            {
                Console.WriteLine("Invalid arguments. Use -f <command_file> to specify the command file.");
            }
        }

        // eg.
        //{
        //  "commands": [
        //    {
        //      "call": "replace_by_content",
        //      "args": {
        //        "file": "文件路径",
        //        "old_str": "旧文本",
        //        "new_str": "新文本"
        //      }
        //    }
        //  ]
        //}
        
        //{
        //  "commands": [
        //    {
        //      "call": "replace_by_lines",
        //      "args": {
        //        "file": "文件路径",
        //        "startLine": 5,
        //        "endLine": 10,
        //        "startLine_str": "起始行校验文本",
        //        "endLine_str": "结束行校验文本",
        //        "new_str": "新的多行内容"
        //      }
        //    }
        //  ]
        //}
        
        /// <summary>
        /// 解析并执行JSON命令
        /// </summary>
        /// <param name="jsonContent">包含命令的JSON字符串</param>
        /// <param name="commandFile">命令文件路径</param>
        private static void eval_command(string jsonContent, string commandFile)
        {
            try
            {
                // 解析JSON文档
                var jsonObject = JObject.Parse(jsonContent);
                
                // 获取commands数组
                var commandsArray = jsonObject["commands"] as JArray;
                if (commandsArray == null)
                {
                    Console.WriteLine("Invalid JSON format: missing or invalid 'commands' array");
                    return;
                }
                
                bool success = true;
                foreach (var commandElement in commandsArray)
                {
                    // 获取工具名称
                    var callElement = commandElement["call"];
                    if (callElement == null)
                    {
                        Console.WriteLine("Invalid JSON format: missing 'call' property");
                        success = false;
                        break;
                    }
                    string toolName = callElement.Value<string>()?.Trim() ?? "";

                    // 获取参数对象
                    var argsElement = commandElement["args"] as JObject;
                    if (argsElement == null)
                    {
                        Console.WriteLine("Invalid JSON format: missing or invalid 'args' object");
                        success = false;
                        break;
                    }

                    // 根据工具名称执行相应的操作
                    bool operationSuccess = false;
                    switch (toolName.ToLower())
                    {
                        case "replace_by_content":
                            operationSuccess = ExecuteReplaceInFileByContent(argsElement);
                            break;
                        case "replace_by_lines":
                            operationSuccess = ExecuteReplaceInFileByLines(argsElement);
                            break;
                        default:
                            Console.WriteLine($"Unsupported tool: {toolName}");
                            operationSuccess = false;
                            break;
                    }
                    
                    // 如果任一操作失败，则整体失败
                    if (!operationSuccess)
                    {
                        success = false;
                    }
                }
                
                // 只有在所有操作都成功时才删除命令文件
                if (success)
                {
                    DeleteCommandFile(commandFile);
                }
                else
                {
                    Environment.Exit(1);
                }
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 删除命令文件
        /// </summary>
        /// <param name="commandFile">命令文件路径</param>
        private static void DeleteCommandFile(string commandFile)
        {
            try
            {
                if (System.IO.File.Exists(commandFile))
                {
                    System.IO.File.Delete(commandFile);
                    Console.WriteLine($"Command file deleted: {commandFile}");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Warning: Failed to delete command file {commandFile}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行文件替换操作
        /// </summary>
        /// <param name="argsElement">包含替换参数的JSON元素</param>
        /// <returns>是否成功执行替换</returns>
        private static bool ExecuteReplaceInFileByContent(JObject argsElement)
        {
            // 获取文件路径参数
            var fileElement = argsElement["file"];
            if (fileElement == null)
            {
                Console.WriteLine("Missing file parameter");
                return false;
            }
            string filePath = fileElement.Value<string>()?.Trim() ?? "";
            
            // 获取旧文本参数
            var oldStrElement = argsElement["old_str"];
            if (oldStrElement == null)
            {
                Console.WriteLine("Missing old_str parameter");
                return false;
            }
            string oldStr = oldStrElement.Value<string>()?.Trim() ?? "";
            
            // 获取新文本参数
            var newStrElement = argsElement["new_str"];
            if (newStrElement == null)
            {
                Console.WriteLine("Missing new_str parameter");
                return false;
            }
            // 获取新文本参数
            var startLine = 0;
            var startLineElement = argsElement["startLine"];
            if (startLineElement != null)
            {
                startLine = startLineElement.Value<int>();
            }
            string newStr = newStrElement.Value<string>()?.Trim() ?? "";
            
            // 执行替换操作 
            return replace_by_content(filePath, oldStr, startLine, newStr);
        }
        
        /// <summary>
        /// 执行按行替换文件操作
        /// </summary>
        /// <param name="argsElement">包含替换参数的JSON元素</param>
        /// <returns>是否成功执行替换</returns>
        private static bool ExecuteReplaceInFileByLines(JObject argsElement)
        {
            // 获取文件路径参数
            var fileElement = argsElement["file"];
            if (fileElement == null)
            {
                Console.WriteLine("Missing file parameter");
                return false;
            }
            string filePath = fileElement.Value<string>()?.Trim() ?? "";
            
            // 获取开始行号参数
            var startLineElement = argsElement["startLine"];
            if (startLineElement == null)
            {
                Console.WriteLine("Missing startLine parameter");
                return false;
            }
            if (!int.TryParse(startLineElement.Value<string>(), out int startLine) || startLine < 1)
            {
                Console.WriteLine("Invalid startLine parameter");
                return false;
            }
            
            // 获取结束行号参数
            var endLineElement = argsElement["endLine"];
            if (endLineElement == null)
            {
                Console.WriteLine("Missing endLine parameter");
                return false;
            }
            if (!int.TryParse(endLineElement.Value<string>(), out int endLine) || (endLine != -1 && endLine < startLine))
            {
                Console.WriteLine("Invalid endLine parameter");
                return false;
            }
            
            // 获取新文本参数
            var newStrElement = argsElement["new_str"];
            if (newStrElement == null)
            {
                Console.WriteLine("Missing new_str parameter");
                return false;
            }
            string newStr = newStrElement.Value<string>()?.Trim() ?? "";
            
            // 获取起始行校验文本参数（必须）
            var startLineStrElement = argsElement["startLine_str"];
            if (startLineStrElement == null)
            {
                Console.WriteLine("Missing startLine_str parameter");
                return false;
            }
            string startLineStr = startLineStrElement.Value<string>()?.Trim() ?? "";
            
            // 校验内容不能为空
            if (string.IsNullOrEmpty(startLineStr))
            {
                Console.WriteLine("startLine_str parameter cannot be empty");
                return false;
            }
            
            // 获取结束行校验文本参数（必须）
            var endLineStrElement = argsElement["endLine_str"];
            if (endLineStrElement == null)
            {
                Console.WriteLine("Missing endLine_str parameter");
                return false;
            }
            string endLineStr = endLineStrElement.Value<string>()?.Trim() ?? "";
            
            // 校验内容不能为空
            if (string.IsNullOrEmpty(endLineStr))
            {
                Console.WriteLine("endLine_str parameter cannot be empty");
                return false;
            }
            
            // 执行替换操作
            return replace_by_lines(filePath, startLine, endLine, newStr, startLineStr, endLineStr);
        }
        private static int indexToLine(String str, int index)
        {
            return str.Substring(0, index).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
        }
        
        // 文件替换方法：将文件中的指定文本替换为新文本
        private static bool replace_by_content(string filePath, string old_str,int startLine, string new_str)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return false;
            }
            old_str = old_str.Replace("\r\n","\n");
            string content = System.IO.File.ReadAllText(filePath).Replace("\r\n","\n");
            var search_lines = old_str.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).SelectMany(x => splitSpeicalMultiline(filePath, x)).ToArray();
            var insert_lines = new_str.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).SelectMany(x => splitSpeicalMultiline(filePath, x)).ToArray();
            var index = content.IndexOf(old_str);
            if (index == -1)
            {
                if (replaceLineByLine(filePath, content,startLine, search_lines, insert_lines))
                {
                    Console.WriteLine($"Replaced at line {indexToLine(content,index)}, deleted {search_lines.Length} lines, inserted {insert_lines.Length} lines");
                    return true;
                }
                Console.WriteLine($"Replacement is not applied: {filePath}");
                return false;
            }
            else if(index != content.LastIndexOf(old_str))
            {
                Console.WriteLine($"Multiple occurrences found: {filePath}");
                return false;
            }
            content = content.Replace(old_str, new_str);
            System.IO.File.WriteAllText(filePath, content);
            Console.WriteLine($"Replaced at line {indexToLine(content, index)}, deleted {search_lines.Length} lines, inserted {insert_lines.Length} lines");
            return true;
        }
        private static int FindFirstLine(string[] source,int sourcePreSkip,string search)
        {
            int rowNumber = sourcePreSkip;
            foreach (var item in source.Skip(rowNumber))
            {
                if (item.TrimStart() == search.TrimStart())
                {
                    return rowNumber;
                }
                rowNumber++;
            }
            return -1;
        }
        private static int FindLastLine(string[] source, int sourcePreSkip, string search)
        {
            int rowNumber = sourcePreSkip;
            foreach (var item in source.Skip(rowNumber))
            {
                if (item.TrimEnd() == search.TrimEnd())
                {
                    return rowNumber;
                }
                rowNumber++;
            }
            return -1;
        }

        private static bool IsLineTextEquals(string source, string search,bool report)
        {
            if(source == search)
            {
                return true;
            }
            if(report)
                Console.WriteLine($"==== This Line is Not Equal ==== \nSOURCE: {source}\n\nSEARCH:{search}\n");
            return false;
        }

        private static bool replaceLineByLine(string filePath, string content,int startLine,string [] search_lines, string [] insert_lines)
        {
            var content_lines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
           
            // check first line
            var startRowNum = FindFirstLine(content_lines, startLine, search_lines[0]);
            if (startRowNum == -1) {
                Console.WriteLine($"First line of searching block is not found in source[{startLine}~{content.Length}]");
                return false;
            }
            if(startRowNum + search_lines.Length > content_lines.Length)
            {
                Console.WriteLine($"Searching block is overflow to the source[{startLine}~{content.Length}]");
                return false;
            }

            
            if (search_lines.Length > 1) {
                // check last line
                var end = FindLastLine(content_lines, startRowNum + search_lines.Length, search_lines[search_lines.Length - 1]);
                if (end == -1)
                {
                    Console.WriteLine("Last line of searching block is not found in source.");
                    return false;
                }
                if (search_lines.Length > 2)
                {
                    // check other lines
                    for (int i = 1; i <= search_lines.Length-1; i++)
                    {
                        if (!IsLineTextEquals(content_lines[startRowNum + i], search_lines[i],false))
                        {
                            Console.WriteLine($"Matched first {i} lines, but matching broke/failed at source line { startRowNum + i + 1}");
                            IsLineTextEquals(content_lines[startRowNum + i], search_lines[i], true);
                            return false;
                        }
                    }
                }
            }
            // all lines matched, do replace
            File.WriteAllText(filePath, string.Join("\n", content_lines.Take(startRowNum)) + "\n" + string.Join("\n", insert_lines) + "\n" + string.Join("\n", content_lines.Skip(startRowNum + search_lines.Length)));
            return true;
        }

        private static string[] splitSpeicalMultiline(string filePath, string x)
        {
            var ext = new FileInfo(filePath).Extension.ToLower();
            if (ext == ".js" || ext == ".tsx" || ext == "ts") {
                if (x.Contains("`"))
                {
                    // if string is wrapped by backtick
                    return x.Split(new string[] { "\\r\\n", "\\n" }, StringSplitOptions.None);
                }
            }
            return new string[] { x };
        }

        // 按行替换文件方法：根据行号范围替换文件内容，支持错位校验
        private static bool replace_by_lines(string filePath, int startLine, int endLine, string new_str, string startLineStr, string endLineStr)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return false;
            }
            
            // 读取文件所有行
            string[] lines = System.IO.File.ReadAllLines(filePath);
            
            // 验证行号范围
            if (startLine > lines.Length)
            {
                Console.WriteLine($"Start line {startLine} exceeds file length {lines.Length}");
                return false;
            }
            
            // 如果endLine为-1，则替换到文件末尾
            if (endLine == -1)
            {
                endLine = lines.Length;
            }
            else if (endLine > lines.Length)
            {
                Console.WriteLine($"End line {endLine} exceeds file length {lines.Length}");
                return false;
            }
            
            // 校验起始行内容
            bool startLineValid = false;
            int actualStartLine = startLine;
            
            // 首先尝试精确匹配
            if (startLine - 1 < lines.Length)
            {
                startLineValid = isMultilineMatchStrictly(startLine, startLineStr, lines);
            }

            int realOffset = 0;
            
            // 如果精确匹配失败，尝试前后错位50行校验
            if (!startLineValid)
            {
                Console.WriteLine($"Start line validation failed at line {startLine}. Expected: '{startLineStr}', Actual: '{lines[startLine - 1].Trim()}'. Attempting offset validation...");
                
                // 向前搜索（最多50行）
                for (int offset = 1; offset <= 50; offset++)
                {
                    int checkLine = startLine - offset;
                    if (checkLine >= 1 && checkLine - 1 < lines.Length)
                    {
                        string actualLineContent = lines[checkLine - 1].Trim();
                        if (actualLineContent == startLineStr)
                        {
                            actualStartLine = checkLine;
                            startLineValid = true;
                            realOffset = -offset;
                            Console.WriteLine($"Start line found at offset -{offset} (line {checkLine})");
                            break;
                        }
                    }
                }
                
                // 如果向前搜索失败，尝试向后搜索（最多50行）
                if (!startLineValid)
                {
                    for (int offset = 1; offset <= 50; offset++)
                    {
                        int checkLine = startLine + offset;
                        if (checkLine >= 1 && checkLine - 1 < lines.Length)
                        {
                            string actualLineContent = lines[checkLine - 1].Trim();
                            if (actualLineContent == startLineStr)
                            {
                                actualStartLine = checkLine;
                                startLineValid = true;
                                realOffset = offset;
                                Console.WriteLine($"Start line found at offset +{offset} (line {checkLine})");
                                break;
                            }
                        }
                    }
                }
            }
            
            if (!startLineValid)
            {
                Console.WriteLine($"Start line validation failed after offset search. Expected: '{startLineStr}'");
                return false;
            }

            
            // 假设替换的行数不变，则结束行也需要相应偏移
            int actualEndLine = actualStartLine + realOffset + (endLine - startLine);
            
            // 校验结束行内容
            bool endLineValid = false;
            if (actualEndLine - 1 < lines.Length)
            {
                endLineValid = isMultilineMatchStrictly(actualEndLine, endLineStr, lines);
            }
            
            // 如果精确匹配失败，尝试前后错位50行校验
            if (!endLineValid)
            {
                Console.WriteLine($"End line validation failed at line {endLine}. \nSEARCH: '{endLineStr}'\nSOURCE: '{lines[actualEndLine - 1].Trim()}'");
                return false;
            }
            
            // 构建新内容
            List<string> newLines = new List<string>();
            
            // 添加开始行之前的内容
            for (int i = 0; i < actualStartLine - 1; i++)
            {
                newLines.Add(lines[i]);
            }
            
            // 添加新内容
            string[] newContentLines = new_str.Split('\n');
            foreach (string line in newContentLines)
            {
                newLines.Add(line.Replace("\r", ""));
            }
            
            // 添加结束行之后的内容
            for (int i = actualEndLine; i < lines.Length; i++)
            {
                newLines.Add(lines[i]);
            }
            
            // 写入文件
            System.IO.File.WriteAllLines(filePath, newLines);
            Console.WriteLine($"Replaced lines {actualStartLine}-{actualEndLine} (originally {startLine}-{endLine}) in: {filePath}");
            return true;
        }

        private static bool isMultilineMatchStrictly(int srcLineIndex, string comparingStr, string[] srcLines)
        {
            
            int compareLineOffset = compareLines.Length;
            for (int i = 0; i < compareLineOffset; i++)
            {
                int currentLineIndex = srcLineIndex - 1 + compareLineOffset;
                if (currentLineIndex > srcLines.Length - 1)
                {
                    Console.WriteLine("Not match due to exceeding file length at line " + (currentLineIndex + 1));
                    return false;
                }
                if (srcLines[currentLineIndex] != compareLines[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}