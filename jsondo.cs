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
            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, ".jsondo"));
            // 显示帮助信息
            if (args.Length == 0 || "/help" == args[0] || "-h" == args[0])
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
                Console.WriteLine("2. replace_by_range: Replace content by line numbers with validation");
                Console.WriteLine("   Example JSON structure:");
                Console.WriteLine("   {");
                Console.WriteLine("     \"commands\": [");
                Console.WriteLine("       {");
                Console.WriteLine("         \"call\": \"replace_by_range\",");
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
                for (int i = 1; i < args.Length; i++)
                {
                    string commandFile = args[i];
                    if (!System.IO.File.Exists(commandFile))
                    {
                        Console.WriteLine("Command file not found: " + commandFile);
                        Console.WriteLine("Stopping execution due to error.");
                        Environment.Exit(1);
                    }
                    string jsonContent = System.IO.File.ReadAllText(commandFile);

                    Console.WriteLine("Eval command from " + commandFile);

                    int result = eval_command(jsonContent, commandFile);
                    if (result != 0)
                    {
                        Console.WriteLine("Command file execution failed: " + commandFile);
                        Console.WriteLine("Stopping execution due to error.");
                        Environment.Exit(1);
                    }
                    Console.WriteLine();
                }
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
        //      "call": "replace_by_range",
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
        /// <returns>返回0表示成功，非0表示失败</returns>
        private static int eval_command(string jsonContent, string commandFile)
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

                    // 获取可选的title字段
                    var titleElement = commandElement["title"];
                    string title = (titleElement != null && titleElement.Type == JTokenType.String) ? titleElement.Value<string>() : null;

                    // 显示当前命令
                    if (!string.IsNullOrEmpty(title))
                    {
                        Console.WriteLine($"  Executing: `{title}`");
                    }

                    // 根据工具名称执行相应的操作
                    bool operationSuccess = false;
                    switch (toolName.ToLower())
                    {
                        case "replace_by_content":
                            operationSuccess = ExecuteReplaceInFileByContent(argsElement);
                            break;
                        case "replace_by_range":
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
                    System.IO.File.Copy(commandFile, Path.Combine(Environment.CurrentDirectory, ".jsondo/jsondo.lastApplied"), true);
                    DeleteCommandFile(commandFile);
                }
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
                return 1;
            }

            return success ? 0 : 1;
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
                    Console.WriteLine($"[OK] All changes from {commandFile}[deleted] are applied.");
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

            // 获取扫描限制参数
            var backwardScanLimit = 10;
            var backwardItem = argsElement["backward_scan_limit"];
            if (backwardItem != null)
            {
                backwardScanLimit = backwardItem.Value<int>();
            }

            var forwardScanLimit = 15;
            var forwardItem = argsElement["forward_scan_limit"];
            if (forwardItem != null)
            {
                forwardScanLimit = forwardItem.Value<int>();
            }

            // 执行替换操作
            return replace_by_content(filePath, oldStr, startLine, newStr, backwardScanLimit, forwardScanLimit);
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
            string startLineStr = startLineStrElement.Value<string>() ?? "";

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
            string endLineStr = endLineStrElement.Value<string>() ?? "";

            // 校验内容不能为空
            if (string.IsNullOrEmpty(endLineStr))
            {
                Console.WriteLine("endLine_str parameter cannot be empty");
                return false;
            }

            // 获取扫描限制参数
            var backwardScanLimit = 10;
            var backwardItem = argsElement["backward_scan_limit"];
            if (backwardItem != null)
            {
                backwardScanLimit = backwardItem.Value<int>();
            }

            var forwardScanLimit = 15;
            var forwardItem = argsElement["forward_scan_limit"];
            if (forwardItem != null)
            {
                forwardScanLimit = forwardItem.Value<int>();
            }

            // 执行替换操作
            return replace_by_range(filePath, startLine, endLine, newStr, startLineStr, endLineStr, backwardScanLimit, forwardScanLimit);
        }
        private static int indexToLine(String str, int index)
        {
            return str.Substring(0, index).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
        }

        // 文件替换方法：将文件中的指定文本替换为新文本
        private static bool replace_by_content(string filePath, string old_str, int startLine, string new_str, int backwardScanLimit, int forwardScanLimit)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"  File not found: {filePath}");
                return false;
            }
            old_str = old_str.Replace("\r\n", "\n");
            string content = System.IO.File.ReadAllText(filePath).Replace("\r\n", "\n");
            var search_lines = old_str.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).SelectMany(x => splitSpeicalMultiline(filePath, x)).ToArray();
            var insert_lines = new_str.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).SelectMany(x => splitSpeicalMultiline(filePath, x)).ToArray();
            var index = content.IndexOf(old_str);
            if (index == -1)
            {
                if (replaceLineByLine(filePath, content, startLine, search_lines, insert_lines, backwardScanLimit, forwardScanLimit))
                {
                    Console.WriteLine($"  Replaced at line {indexToLine(content, index)}, deleted {search_lines.Length} lines, inserted {insert_lines.Length} lines");
                    return true;
                }
                Console.WriteLine($"  Replacement is not applied: {filePath}");
                return false;
            }
            else if (index != content.LastIndexOf(old_str))
            {
                Console.WriteLine($"  Multiple occurrences found: {filePath}");
                return false;
            }
            content = content.Replace(old_str, new_str);
            System.IO.File.Copy(filePath, Path.Combine(Environment.CurrentDirectory, ".jsondo/jsondo.lastbackup"),true);
            System.IO.File.WriteAllText(filePath, content);
            Console.WriteLine($"  Replaced at line {indexToLine(content, index)}, deleted {search_lines.Length} lines, inserted {insert_lines.Length} lines");
            return true;
        }
        private static int FindFirstLine(string[] source, int sourcePreSkip, string search,int searchDistance=10)
        {
            int rowNumber = sourcePreSkip;
            int i = 0;
            foreach (var item in source.Skip(rowNumber))
            {
                if (item.TrimStart() == search.TrimStart())
                {
                    return rowNumber;
                }
                rowNumber++;
                i++;
                if(i> searchDistance)
                {
                    break;
                }
            }
            return -1;
        }
        private static int FindLastLine(string[] source, int sourcePreSkip, string search, int searchDistance = 30)
        {
            int rowNumber = sourcePreSkip;
            int i = 0;
            foreach (var item in source.Skip(rowNumber))
            {
                if (item.TrimEnd() == search.TrimEnd())
                {
                    return rowNumber;
                }
                rowNumber++;
                if (i > searchDistance)
                {
                    break;
                }
            }
            return -1;
        }

        private static bool IsLineTextEquals(string source, string search, int reportLine)
        {
            if (source == search)
            {
                return true;
            }
            if (reportLine!=-1)
                Console.WriteLine($"==== LN-{reportLine}: This Line is Not Equal ==== \nREQEUSTED: {search}\n\nACTRUALLY:{source}\n");
            return false;
        }

        private static bool replaceLineByLine(string filePath, string content, int startLine, string[] search_lines, string[] insert_lines, int backwardScanLimit, int forwardScanLimit)
        {
            var content_lines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            // check first line
            int searchStartLine = startLine - backwardScanLimit;
            if (searchStartLine < 0) searchStartLine = 0;
            int searchEndLine = startLine + forwardScanLimit;

            var startRowNo = -1;
            for (int i = searchStartLine; i <= searchEndLine && i < content_lines.Length; i++)
            {
                if (content_lines[i] == search_lines[0])
                {
                    startRowNo = i;
                    break;
                }
            }

            if (startRowNo == -1)
            {
                Console.WriteLine($"  E: First line mismatch near LN-{startLine} (±{backwardScanLimit + forwardScanLimit} lines)");
                return false;
            }
            if (startRowNo + search_lines.Length > content_lines.Length)
            {
                Console.WriteLine($"  E: Total lines of the searching content is more than the rest lines of source");
                Console.WriteLine($"  Searching lines sum: {search_lines.Length}, but {content_lines.Length - startLine} lines from LN-{startLine} to the source.");
                return false;
            }


            if (search_lines.Length > 1)
            {
                // check last line
                var searchEndStart = startRowNo + search_lines.Length;
                var end = -1;
                for (int i = searchEndStart; i < searchEndStart + forwardScanLimit && i < content_lines.Length; i++)
                {
                    if (content_lines[i] == search_lines[search_lines.Length - 1])
                    {
                        end = i;
                        break;
                    }
                }

                if (end == -1)
                {
                    Console.WriteLine($"  Last line mismatch near LN-{startRowNo + search_lines.Length}.");
                    return false;
                }
                if (search_lines.Length > 2)
                {
                    // check other lines
                    for (int i = 1; i <= search_lines.Length - 1; i++)
                    {
                        if (!IsLineTextEquals(content_lines[startRowNo + i], search_lines[i], -1))
                        {
                            Console.WriteLine($"  Matched first {i} lines, but mismatch at at LN-{startRowNo + i}");
                            IsLineTextEquals(content_lines[startRowNo + i], search_lines[i], startRowNo + i);
                            return false;
                        }
                    }
                }
            }
            // all lines matched, do replace
            File.WriteAllText(filePath, string.Join("\n", content_lines.Take(startRowNo)) + "\n" + string.Join("\n", insert_lines) + "\n" + string.Join("\n", content_lines.Skip(startRowNo + search_lines.Length)));
            return true;
        }

        private static string[] splitSpeicalMultiline(string filePath, string x)
        {
            var ext = new FileInfo(filePath).Extension.ToLower();
            if (ext == ".js" || ext == ".tsx" || ext == ".ts")
            {
                if (x.Contains("`"))
                {
                    // if string is wrapped by backtick
                    return x.Split(new string[] { "\\r\\n", "\\n" }, StringSplitOptions.None);
                }
            }
            return new string[] { x };
        }

        // 按行替换文件方法：根据行号范围替换文件内容，支持错位校验
        private static bool replace_by_range(string filePath, int startLineNo, int endLineNo, string new_str, string startLineStr, string endLineStr, int backwardScanLimit, int forwardScanLimit)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"  File not found: {filePath}");
                return false;
            }

            // 读取文件所有行
            string[] lines = System.IO.File.ReadAllLines(filePath);

            // 验证行号范围
            if (startLineNo > lines.Length)
            {
                Console.WriteLine($"  Start line {startLineNo} exceeds file length {lines.Length}");
                return false;
            }

            // 如果endLine为-1，则替换到文件末尾
            if (endLineNo == -1)
            {
                endLineNo = lines.Length;
            }
            else if (endLineNo > lines.Length)
            {
                Console.WriteLine($"  End line {endLineNo} exceeds file length {lines.Length}");
                return false;
            }

            // 校验起始行内容
            int actualStartLineNo = startLineNo;

            int minStartLineNoOfEndMarker = startLineNo+1;
            if (!IsMultiLinesEqual(startLineNo, startLineStr, lines))
            {
                // 如果精确匹配失败，尝试从前面backwardScanLimit行开始执行内容搜索
                string[] searchingLines = startLineStr.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                int markerStartIndex = LocateMultiLinesBackward(searchingLines, lines, startLineNo, backwardScanLimit);
                if (markerStartIndex == -1)
                {
                    // 继续，向文档后部查找
                    markerStartIndex = LocateMultiLinesForward(searchingLines, lines, startLineNo, forwardScanLimit);
                }
                if (markerStartIndex == -1)
                {
                    Console.WriteLine($"  W: Start marker not found near LN-{startLineNo} (±{backwardScanLimit + forwardScanLimit} lines). \n  REQEUSTED: '{startLineStr}'\n  ACTRUALLY: '{lines[actualStartLineNo - 1]}'");
                    return false;
                }
                actualStartLineNo = markerStartIndex + 1;
                minStartLineNoOfEndMarker = actualStartLineNo + searchingLines.Length;
            }

            int actualEndLineNo = endLineNo; // 假设请求的结束位置是实际的。
            if (!IsMultiLinesEqual(endLineNo, endLineStr, lines))
            {
                // 如果精确匹配失败，尝试前后forwardScanLimit行校验
                string[] searchingLines = endLineStr.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                int markerStartIndex = LocateMultiLinesForward(searchingLines, lines, minStartLineNoOfEndMarker, forwardScanLimit);
                if (markerStartIndex == -1)
                {
                    Console.WriteLine($"  WARN: End marker not found within {forwardScanLimit} lines after LN-{endLineNo}. \n  REQEUSTED: '{endLineStr}'\n  ACTRUALLY: '{lines[actualEndLineNo - 1]}'");
                    return false;
                }
                // 调整实际位置
                actualEndLineNo = markerStartIndex + 1 + searchingLines.Length;
                Console.WriteLine($"  INFO: Searching extended, found end marker at LN-{markerStartIndex + 1} instead of LN-{endLineNo}");
            }

            // 构建新内容
            List<string> newLines = new List<string>();

            // 添加开始行之前的内容
            for (int i = 0; i < actualStartLineNo - 1; i++)
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
            for (int i = actualEndLineNo; i < lines.Length; i++)
            {
                newLines.Add(lines[i]);
            }
            System.IO.File.Copy(filePath, Path.Combine(Environment.CurrentDirectory, ".jsondo/jsondo.lastbackup"),true);
            
            // 写入文件
            System.IO.File.WriteAllLines(filePath, newLines);
            if (actualStartLineNo != startLineNo || actualEndLineNo != endLineNo)
            {
                Console.WriteLine($"  Replaced {actualEndLineNo-actualStartLineNo} lines LN{actualStartLineNo}~{actualEndLineNo} (adjusted from requested LN{startLineNo}~{endLineNo}) in: {filePath}");
            }
            else
            {
                Console.WriteLine($"  Replaced {endLineNo-startLineNo} lines LN{startLineNo}~{endLineNo}] successfully in: {filePath}");
            }
            return true;
        }

        private static bool IsMultiLinesEqual(int srcLineIndex, string comparingStr, string[] srcLines)
        {
            if(srcLineIndex - 1 >= srcLines.Length)
            {
                return false;
            }
            var compareLines = comparingStr.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            int compareLineOffset = compareLines.Length;
            for (int i = 0; i < compareLineOffset; i++)
            {
                int currentLineIndex = srcLineIndex - 1 + i;
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

        private static int LocateMultiLinesForward(string[] searchLines, string[] sourceLines, int sourceStartLN, int forward = 50)
        {
            int rangStart = -1;
            if (sourceStartLN < 0)
            {
                sourceStartLN = 0;
            }
            int offset = 0;
            for (int i = sourceStartLN; i < Math.Min(sourceLines.Length, sourceStartLN + forward); i++)
            {
                if (sourceLines[i] != searchLines[offset])
                {
                    offset = 0;
                    rangStart = -1;
                    continue;
                }
                else
                {
                    offset++;
                    if (rangStart == -1)
                        rangStart = i;

                }

                if (offset == searchLines.Length)
                {
                    break;
                }
            }
            return rangStart;
        }

        private static int LocateMultiLinesBackward(string[] searchLines, string[] sourceLines, int sourceStart, int backward = 50)
        {
            int rangStart = -1;
            int processed = 0;
            int fromLineIndex = sourceStart - backward < 0 ? 0 : sourceStart - backward;
            for (int i = sourceStart - 1; i >= fromLineIndex; i--)
            {
                if (sourceLines[i] != searchLines[searchLines.Length - processed - 1])
                {
                    processed = 0;
                    rangStart = -1;
                    continue;
                }
                else
                {
                    processed++;
                    rangStart = i;

                }

                if (processed == searchLines.Length)
                {
                    break;
                }
            }
            return rangStart;
        }
    }
}