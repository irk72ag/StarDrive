﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Ship_Game.Data
{
    // Simplified text parser for StarDrive data files
    public class StarDataParser : IDisposable
    {
        TextReader Reader;
        public StarDataNode Root { get; }
        int Line;
        readonly StringBuilder StrBuilder = new StringBuilder();

        public StarDataParser(string file) : this(ResourceManager.GetModOrVanillaFile(file))
        {
        }

        public StarDataParser(FileInfo f) : this(f?.NameNoExt(), OpenStream(f))
        {
        }

        public StarDataParser(string name, TextReader reader)
        {
            Reader = reader;
            Root = new StarDataNode { Key = name ?? "", Value = null };
            Parse();
        }
        
        static StreamReader OpenStream(FileInfo f, string nameInfo = null)
        {
            if (f == null || !f.Exists)
                throw new FileNotFoundException($"Required StarData file not found! {nameInfo ?? f?.FullName}");
            try
            {
                return new StreamReader(f.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            }
            catch (UnauthorizedAccessException e) // file is still open?
            {
                Log.Warning(ConsoleColor.DarkRed, $"Open failed: {e.Message} {nameInfo ?? f.FullName}");
                Thread.Sleep(1); // wait a bit
            }
            // try again or throw
            return new StreamReader(f.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
        }

        public void Dispose()
        {
            Reader?.Close(); Reader = null;
        }

        struct DepthSave
        {
            public int Depth;
            public StarDataNode Node;
        }
        
        void Parse()
        {
            Line = 0;
            int depth = 0;
            string currentLine;
            var saved = new Stack<DepthSave>();

            StarDataNode root = Root;
            StarDataNode prev = Root;

            while ((currentLine = Reader.ReadLine()) != null)
            {
                Line += 1;
                var view = new StringView(currentLine);
                view.SkipWhiteSpace(out int newDepth);

                // "      " -- line is all spaces
                // "  # comment after spaces "
                if (view.Length == 0 || view.Char0 == '#')
                    continue;

                StarDataNode node = ParseLineAsNode(ref view, ref newDepth, out bool isSequence);
                if (node == null)
                    continue;

                if (newDepth > depth)
                {
                    saved.Push(new DepthSave{ Depth=depth, Node=root });
                    root = prev; // root changed
                }
                else if (newDepth < depth)
                {
                    for (;;) // try to pop down until we get to right depth
                    {
                        DepthSave save = saved.Pop();
                        if (save.Depth > newDepth && saved.Count > 0)
                            continue;
                        root = save.Node;
                        break;
                    }
                }

                if (isSequence)
                {
                    // we got a sequence element
                    // root:
                    //   - node
                    root.AddSequenceElement(node);
                }
                else
                {
                    // we got a sub node
                    // root:
                    //   node
                    root.AddSubNode(node);
                }

                depth = newDepth;
                prev = node;
            }
        }

        StarDataNode ParseLineAsNode(ref StringView view, ref int newDepth, out bool isSequence)
        {
            isSequence = view.StartsWith("- ");
            if (isSequence)
            {
                newDepth += 2;
                view.Skip(2);
                if (view.Length == 0)
                {
                    Error(view, "Syntax Error: expected a value");
                    return null;
                }
            }
            var node = new StarDataNode { Key = "" };
            return ParseTokenAsNode(node, ref view);
        }

        StarDataNode ParseTokenAsNode(StarDataNode node, ref StringView view)
        {
            StringView first = NextToken(ref view);
            if (first.Length == 0)
                return node; // completely empty node (allowed by YAML spec)

            if (first.Char0 == '{')
            {
                ParseObject(node, ref view);
                return node;
            }

            StringView second = NextToken(ref view);
            if (second.Length == 0) // only value!
            {
                node.Value = ParseKey(first, ref view);
                return node;
            }
            if (second != ":")
            {
                Error(second, $"Syntax Error: expected ':' for key:value entry but got {second.Text} instead");
                return node;
            }

            node.Key = ParseKey(first, ref view);

            StringView third = NextToken(ref view);
            third.TrimEnd();
            if (third.Length == 0) // no value! (probably a sequence will follow)
                return node;
            
            if (third.Char0 == '{')
            {
                ParseObject(node, ref view);
            }
            else
            {
                node.Value = ParseValue(third, ref view);
            }
            return node;
        }

        object ParseKey(in StringView token, ref StringView view)
        {
            switch (token.Char0)
            {
                // because I don't want to support this extra complexity
                case '{': Error(token, "Syntax Restriction: maps not allowed as keys");   return null;
                case '[': Error(token, "Syntax Restriction: arrays not allowed as keys"); return null;
                default: return ParseValue(token, ref view);
            }
        }

        object ParseValue(StringView token, ref StringView view)
        {
            if (token.Length == 0) return null;
            if (token == "null")   return null;
            if (token == "true")   return true;
            if (token == "false")  return false;
            char c = token.Char0;
            if (('0' <= c && c <= '9') || c == '-' || c == '+')
            {
                if (token.IndexOf('.') != -1)
                    return token.ToDouble();
                return token.ToInt();
            }
            if (c == '\'' || c == '"')
            {
                return ParseString(ref view, terminator: c);
            }
            if (c == '[')
            {
                return ParseArray(ref view);
            }
            if (c == '{')
            {
                Error(token, "Parse Error: map not supported in this context");
                return null;
            }
            token.TrimEnd();
            return token.Text; // probably some text
        }
        
        void ParseObject(StarDataNode node, ref StringView view)
        {
            for (;;)
            {
                view.TrimStart();
                if (view.Length == 0)
                {
                    Error(view, "Parse Error: map expected '}' before end of line");
                    break;
                }

                if (view.Char0 == '}')
                    break; // end of map

                var child = new StarDataNode();
                ParseTokenAsNode(child, ref view);
                node.AddSubNode(child);

                StringView separator = NextToken(ref view);
                if (separator.Length == 0)
                {
                    Error(separator, "Parse Error: map expected '}' before end of line");
                    break;
                }

                if (separator.Char0 == '}')
                    break; // end of map

                if (separator.Char0 != ',')
                {
                    Error(separator, "Parse Error: map expected ',' separator after value entry");
                    break;
                }
            }
        }

        object ParseArray(ref StringView view)
        {
            var arrayItems = new Array<object>();

            for (;;)
            {
                StringView token = NextToken(ref view);
                if (token.Length == 0)
                {
                    Error(token, "Parse Error: array expected ']' before end of line");
                    break;
                }
                
                if (token.Char0 == ']')
                    break; // end of array

                object o = ParseValue(token, ref view);
                arrayItems.Add(o);

                StringView separator = NextToken(ref view);
                if (separator.Length == 0)
                {
                    Error(separator, "Parse Error: array expected ']' before end of line");
                    break;
                }

                if (separator.Char0 == ']')
                    break; // end of array

                if (separator.Char0 != ',')
                {
                    Error(separator, "Parse Error: array expected ',' separator after an entry");
                    break;
                }
            }
            return arrayItems.ToArray();
        }

        string ParseString(ref StringView view, char terminator)
        {
            StrBuilder.Clear();
            while (view.Length > 0)
            {
                char c = view.PopFirst();
                if (c == terminator)
                    break;

                if (c == '\\')
                {
                    if (view.Length > 0)
                    {
                        c = view.PopFirst();
                        switch (c)
                        {
                            case '\\': StrBuilder.Append('\\'); break;
                            case 't':  StrBuilder.Append('\t'); break;
                            case 'n':  StrBuilder.Append('\n'); break;
                            case 'r':  StrBuilder.Append('\r'); break;
                            case '\'': StrBuilder.Append('\''); break;
                            case '"':  StrBuilder.Append('"');  break;
                        }
                    }
                    else
                    {
                        Error(view, "Parse Error: unexpected end of string");
                        break;
                    }
                }
                else
                {
                    StrBuilder.Append(c);
                }
            }
            return StrBuilder.ToString();
        }

        static StringView NextToken(ref StringView view)
        {
            view.TrimStart();

            int start = view.Start;
            int current = start;
            int eos = start + view.Length;
            string str = view.Str;
            while (current < eos)
            {
                switch (str[current])
                {
                    case ':': case '\'': case '"': case '#': case ',':
                    case '{': case '}':  case '[': case ']':
                        if (start == current)
                        {
                            view.Skip(1);
                            return new StringView(start, 1, str);
                        }
                        goto finished;
                }
                ++current;
            }

            finished:
            int length = current - start;
            view.Skip(length);
            return new StringView(start, length, str);
        }

        void Error(in StringView view, string what)
        {
            Log.Error($"{Root.Key}:{Line}:{view.Start} {what}");
        }

        public Array<T> DeserializeArray<T>() where T : new()
        {
            var items = new Array<T>();
            var ser = new StarDataSerializer(typeof(T));
            foreach (StarDataNode child in Root)
            {
                items.Add((T)ser.Deserialize(child));
            }
            return items;
        }
    }
}
