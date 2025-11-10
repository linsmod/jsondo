using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace search_replace
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 显示帮助信息
            if(args.Length == 0 || "/help" == args[0] || "-h"== args[0])
            {
                Console.WriteLine("Usage: xmldo -f <command_file>");
                Console.WriteLine("The command file should contain XML instructions for the tool to execute. For example:");
                Console.WriteLine("<xmldo>");
                Console.WriteLine("  <call>replace_by_content</call>");
                Console.WriteLine("  <args>");
                Console.WriteLine("    <file>path/to/file.txt</file>");
                Console.WriteLine("    <old_str>old text</old_str>");
                Console.WriteLine("    <new_str>new text</new_str>");
                Console.WriteLine("  </args>");
                Console.WriteLine("</xmldo>");
                Console.WriteLine();
                return;
            }
            
            // 解析 -f 参数并执行命令文件
            if (args[0] == "-f" && args.Length > 1)
            {
                string commandFile = args[1];
                string xml = System.IO.File.ReadAllText(commandFile);
                eval_command(xml, commandFile);
            }
            else
            {
                Console.WriteLine("Invalid arguments. Use -f <command_file> to specify the command file.");
            }
        }

        // eg.
        //<xmldo>
        //<call>replace_by_content</call>
        //<args>
        //<file>文件路径</file>
        //<old_str>旧文本</old_str>
        //<new_str>新文本</new_str>
        //</args>
        //</xmldo>
        /// 
        
        //<xmldo>
        //<call>replace_by_lines</call>
        //<args>
        //<file>文件路径</file>
        //<startLine>要删除的内容的开始行号，行号从1开始</startLine>
        //<endLine>要删除的内容的结束行号，包含该行。如果为-1，则删除到文件末尾</endLine>
        //<new_str>新文本</new_str>
        //</args>
        //</xmldo>
        /// 
        /// <summary>
        /// 解析并执行XML命令
        /// </summary>
        /// <param name="xml">包含命令的XML字符串</param>
        /// <param name="commandFile">命令文件路径</param>
        private static void eval_command(string xml, string commandFile)
        {
            try
            {
                // 创建XML文档对象
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.LoadXml("<root>"+xml+"</root>");
                
                // 获取xmldo节点
                System.Xml.XmlNodeList xmldoNodes = doc.SelectNodes("/root/xmldo");
                if (xmldoNodes.Count == 0)
                {
                    Console.WriteLine("Invalid XML format: missing <xmldo> element");
                    return;
                }
                bool success = true;
                foreach (System.Xml.XmlNode xmldoNode in xmldoNodes)
                {
                    // 获取工具名称
                    System.Xml.XmlNode callNode = xmldoNode.SelectSingleNode("call");
                    if (callNode == null)
                    {
                        Console.WriteLine("Invalid XML format: missing <call> element");
                        success = false;
                        break;
                    }
                    string toolName = callNode.InnerText.Trim();

                    // 获取参数节点
                    System.Xml.XmlNode argsNode = xmldoNode.SelectSingleNode("args");
                    if (argsNode == null)
                    {
                        Console.WriteLine("Invalid XML format: missing <args> element");
                        success = false;
                        break;
                    }

                    // 根据工具名称执行相应的操作
                    bool operationSuccess = false;
                    switch (toolName.ToLower())
                    {
                        case "replace_by_content":
                            operationSuccess = ExecuteReplaceInFileByContent(argsNode);
                            break;
                        case "replace_by_lines":
                            operationSuccess = ExecuteReplaceInFileByLines(argsNode);
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
            }
            catch (System.Xml.XmlException ex)
            {
                Console.WriteLine($"XML parsing error: {ex.Message}");
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
                Console.WriteLine($"Warning: Failed to delete command file {commandFile}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 执行文件替换操作
        /// </summary>
        /// <param name="argsNode">包含替换参数的XML节点</param>
        /// <returns>是否成功执行替换</returns>
        private static bool ExecuteReplaceInFileByContent(System.Xml.XmlNode argsNode)
        {
            // 获取文件路径参数
            System.Xml.XmlNode fileNode = argsNode.SelectSingleNode("file");
            if (fileNode == null)
            {
                Console.WriteLine("Missing file parameter");
                return false;
            }
            string filePath = fileNode.InnerText.Trim();
            
            // 获取旧文本参数
            System.Xml.XmlNode oldStrNode = argsNode.SelectSingleNode("old_str");
            if (oldStrNode == null)
            {
                Console.WriteLine("Missing old_str parameter");
                return false;
            }
            string oldStr = oldStrNode.InnerText.Trim();
            
            // 获取新文本参数
            System.Xml.XmlNode newStrNode = argsNode.SelectSingleNode("new_str");
            if (newStrNode == null)
            {
                Console.WriteLine("Missing new_str parameter");
                return false;
            }
            string newStr = newStrNode.InnerText.Trim();
            
            // 执行替换操作
            return replace_by_content(filePath, oldStr, newStr);
        }
        
        /// <summary>
        /// 执行按行替换文件操作
        /// </summary>
        /// <param name="argsNode">包含替换参数的XML节点</param>
        /// <returns>是否成功执行替换</returns>
        private static bool ExecuteReplaceInFileByLines(System.Xml.XmlNode argsNode)
        {
            // 获取文件路径参数
            System.Xml.XmlNode fileNode = argsNode.SelectSingleNode("file");
            if (fileNode == null)
            {
                Console.WriteLine("Missing file parameter");
                return false;
            }
            string filePath = fileNode.InnerText.Trim();
            
            // 获取开始行号参数
            System.Xml.XmlNode startLineNode = argsNode.SelectSingleNode("startLine");
            if (startLineNode == null)
            {
                Console.WriteLine("Missing startLine parameter");
                return false;
            }
            if (!int.TryParse(startLineNode.InnerText.Trim(), out int startLine) || startLine < 1)
            {
                Console.WriteLine("Invalid startLine parameter");
                return false;
            }
            
            // 获取结束行号参数
            System.Xml.XmlNode endLineNode = argsNode.SelectSingleNode("endLine");
            if (endLineNode == null)
            {
                Console.WriteLine("Missing endLine parameter");
                return false;
            }
            if (!int.TryParse(endLineNode.InnerText.Trim(), out int endLine) || (endLine != -1 && endLine < startLine))
            {
                Console.WriteLine("Invalid endLine parameter");
                return false;
            }
            
            // 获取新文本参数
            System.Xml.XmlNode newStrNode = argsNode.SelectSingleNode("new_str");
            if (newStrNode == null)
            {
                Console.WriteLine("Missing new_str parameter");
                return false;
            }
            string newStr = newStrNode.InnerText.Trim();
            
            // 执行替换操作
            return replace_by_lines(filePath, startLine, endLine, newStr);
        }
        // 文件替换方法：将文件中的指定文本替换为新文本
        private static bool replace_by_content(string filePath, string old_str, string new_str)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return false;
            }
            old_str = old_str.Replace("\r\n","\n");
            string content = System.IO.File.ReadAllText(filePath).Replace("\r\n","\n");
            
            var index = content.IndexOf(old_str);
            if (index == -1)
            {
                Console.WriteLine($"String not found: {filePath}");
                return false;
            }
            else if(index != content.LastIndexOf(old_str))
            {
                Console.WriteLine($"Multiple occurrences found: {filePath}");
                return false;
            }
            content = content.Replace(old_str, new_str);
            System.IO.File.WriteAllText(filePath, content);
            Console.WriteLine($"Replaced text in: {filePath}");
            return true;
        }
        
        // 按行替换文件方法：根据行号范围替换文件内容
        private static bool replace_by_lines(string filePath, int startLine, int endLine, string new_str)
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
            
            // 构建新内容
            List<string> newLines = new List<string>();
            
            // 添加开始行之前的内容
            for (int i = 0; i < startLine - 1; i++)
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
            for (int i = endLine; i < lines.Length; i++)
            {
                newLines.Add(lines[i]);
            }
            
            // 写入文件
            System.IO.File.WriteAllLines(filePath, newLines);
            Console.WriteLine($"Replaced lines {startLine}-{endLine} in: {filePath}");
            return true;
        }
    }
}
