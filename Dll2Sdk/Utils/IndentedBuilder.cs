using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dll2Sdk.Utils
{
    public class IndentedBuilder
    {
        private readonly Stack<int> _stack = new Stack<int>();
        private int _tabCount;
        private readonly StringBuilder _builder = new StringBuilder();

        public void Push()
        {
            _stack.Push(_tabCount);
        }

        public void Pop()
        {
            _tabCount = _stack.Pop();
        }
        
        public void Indent()
        {
            _tabCount++;
        }

        public void Outdent()
        {
            if (_tabCount > 0)
             _tabCount--;
        }

        public void AppendNewLine()
        {
            _builder.Append(Environment.NewLine);
        }
        
        public void AppendLine(string str)
        {
            _builder.Append(str);
            AppendNewLine();
        }

        public void AppendIndented(string str)
        {
            _builder.Append('\t', _tabCount);
            _builder.Append(str);
        }

        public void AppendIndentedLine(string str)
        {
            _builder.Append('\t', _tabCount);
            _builder.Append(str);
            AppendNewLine();
        }
        
        public void Append(string str) => _builder.Append(str);

        public override string ToString() => _builder.ToString().Trim() + Environment.NewLine;
    }
}
