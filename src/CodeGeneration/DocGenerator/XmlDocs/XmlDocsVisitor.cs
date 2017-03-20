using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AsciiDocNet;
using NuDoq;

namespace DocGenerator.XmlDocs
{
    public class XmlDocsVisitor : Visitor
    {
        private LabeledListItem _labeledListItem;
        private readonly Type _type;
        private const string ListItemContinuation = "\r\n+\r\n";

        public List<LabeledListItem> LabeledListItems { get; } = new List<LabeledListItem>();

        public XmlDocsVisitor(Type type)
        {
            _type = type;
        }

        public override void VisitText(Text text)
        {
            var content = text.Content.Trim();
            if (!_labeledListItem.Any())
                _labeledListItem.Add(new Paragraph(content));
            else
            {
                var paragraph = _labeledListItem.Last() as Paragraph;

                if (paragraph == null)
                    _labeledListItem.Add(new Paragraph(content));
                else
                {
                    var literal = paragraph.Last() as TextLiteral;

                    if (literal != null && literal.Text == ListItemContinuation)
                        paragraph.Add(new TextLiteral(content));
                    else
                        paragraph.Add(new TextLiteral(" " + content));
                }
            }
        }

        public override void VisitParam(Param param)
        {
            // TODO: add to docs
        }

        public override void VisitPara(Para para)
        {
            var paragraph = _labeledListItem.LastOrDefault() as Paragraph;
            paragraph?.Add(new TextLiteral(ListItemContinuation));
            base.VisitPara(para);
        }

        public override void VisitC(C code)
        {
            var content = EncloseInMarks(code.Content.Trim());
            if (!_labeledListItem.Any())
            {
                _labeledListItem.Add(new Paragraph(content));
            }
            else
            {
                var paragraph = _labeledListItem.Last() as Paragraph;
                if (paragraph == null)
                    _labeledListItem.Add(new Paragraph(content));
                else
                    paragraph.Add(new TextLiteral(" " + content));
            }
        }

        public override void VisitSee(See see)
        {
            var content = EncloseInMarks(ExtractLastTokenAndFillGenericParameters((see.Cref ?? see.Content).Trim()));
            if (!_labeledListItem.Any())
            {
                _labeledListItem.Add(new Paragraph(content));
            }
            else
            {
                var paragraph = _labeledListItem.Last() as Paragraph;

                if (paragraph == null)
                    _labeledListItem.Add(new Paragraph(content));
                else
                    paragraph.Add(new TextLiteral(" " + content));
            }
        }

        private string ExtractLastTokenAndFillGenericParameters(string value)
        {
            if (value == null)
                return string.Empty;

            var endOfToken = value.IndexOf("(");
            if (endOfToken == -1)
                endOfToken = value.Length;

            var index = 0;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '.')
                    index = i + 1;
                else if (value[i] == '(')
                    break;
            }

            var length = endOfToken - index;
            var lastToken = value.Substring(index, length);

            var indexOfBackTick = lastToken.IndexOf("`");
            if (indexOfBackTick > -1)
            {
                var arity = lastToken[indexOfBackTick + 1];
                lastToken = lastToken.Substring(0, indexOfBackTick);

                return Enumerable.Range(1, int.Parse(arity.ToString()))
                    .Aggregate(lastToken + "<", (l, i) => l = l + (i == 1 ? "T" : $"T{i}")) + ">";
            }

            return lastToken;
        }

        private string EncloseInMarks(string value) => $"`{value}`";

        public override void VisitMember(Member member)
        {
            if (member.Info != null)
            {
                if (member.Info.DeclaringType == _type &&
                    member.Info.MemberType.HasFlag(MemberTypes.Method))
                {
                    var methodInfo = member.Info as MethodInfo;

                    if (methodInfo != null && methodInfo.IsPublic)
                    {
                        if (_labeledListItem != null)
                            LabeledListItems.Add(_labeledListItem);

                        _labeledListItem = new LabeledListItem(EncloseInMarks(methodInfo.GetSignature()), 0);
                        base.VisitMember(member);
                    }
                }
            }
        }
    }

    public static class MethodInfoExtensions
    {
        /// <summary>
        /// Return the method signature as a string.
        /// </summary>
        /// <param name="method">The Method</param>
        /// <param name="callable">Return as an callable string(public void a(string b) would return a(b))</param>
        /// <returns>Method signature</returns>
        public static string GetSignature(this MethodInfo method, bool callable = false)
        {
            var firstParam = true;
            var sigBuilder = new StringBuilder();
            sigBuilder.Append(method.Name);

            // Add method generics
            if (method.IsGenericMethod)
            {
                sigBuilder.Append("<");
                foreach (var g in method.GetGenericArguments())
                {
                    if (firstParam)
                        firstParam = false;
                    else
                        sigBuilder.Append(", ");
                    sigBuilder.Append(TypeName(g));
                }
                sigBuilder.Append(">");
            }
            sigBuilder.Append("(");
            firstParam = true;
            var secondParam = false;
            foreach (var param in method.GetParameters())
            {
                if (firstParam)
                {
                    firstParam = false;
                    if (method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                    {
                        if (callable)
                        {
                            secondParam = true;
                            continue;
                        }
                        sigBuilder.Append("this ");
                    }
                }
                else if (secondParam == true)
                    secondParam = false;
                else
                    sigBuilder.Append(", ");
                if (param.ParameterType.IsByRef)
                    sigBuilder.Append("ref ");
                else if (param.IsOut)
                    sigBuilder.Append("out ");
                if (!callable)
                {
                    sigBuilder.Append(TypeName(param.ParameterType));
                    sigBuilder.Append(' ');
                }
                sigBuilder.Append(param.Name);

                if (param.HasDefaultValue)
                {
                    object defaultValue;
                    if (param.ParameterType == typeof(bool))
                    {
                        defaultValue = (bool) param.DefaultValue ? "true" : "false";
                    }
                    else
                    {
                        defaultValue = param.DefaultValue;
                    }

                    sigBuilder.Append(" = " + defaultValue);
                }

            }
            sigBuilder.Append(")");
            return sigBuilder.ToString();
        }

        /// <summary>
        /// Get full type name with full namespace names
        /// </summary>
        /// <param name="type">Type. May be generic or nullable</param>
        /// <returns>Full type name, fully qualified namespaces</returns>
        public static string TypeName(Type type)
        {
            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return nullableType.Name + "?";

            if (!type.IsGenericType)
                switch (type.Name)
                {
                    case "String": return "string";
                    case "Int32": return "int";
                    case "Decimal": return "decimal";
                    case "Object": return "object";
                    case "Void": return "void";
                    default:
                        {
                            return string.IsNullOrWhiteSpace(type.FullName) ? type.Name : type.FullName;
                        }
                }

            var sb = new StringBuilder(type.Name.Substring(0,
            type.Name.IndexOf('`'))
            );
            sb.Append('<');
            var first = true;
            foreach (var t in type.GetGenericArguments())
            {
                if (!first)
                    sb.Append(',');
                sb.Append(TypeName(t));
                first = false;
            }
            sb.Append('>');
            return sb.ToString();
        }

    }
}