// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler.XmlDoc;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace dnSpy.Decompiler.ILSpy.Core.XmlDoc {
	/// <summary>
	/// Adds XML documentation for member definitions.
	/// </summary>
	struct AddXmlDocTransform {
		readonly StringBuilder stringBuilder;

		public AddXmlDocTransform(StringBuilder sb) => stringBuilder = sb;

		public void Run(AstNode node) {
			try {
				foreach (var entity in node.DescendantsAndSelf.OfType<EntityDeclaration>()) {
					var symbol = entity.GetSymbol();
					dnlib.DotNet.IMemberRef? mr;
					switch (symbol) {
					case IMember member:
						mr = member.MetadataToken as IMemberRef;
						break;
					case ICSharpCode.Decompiler.TypeSystem.IType type:
						mr = type.GetDefinition()?.MetadataToken;
						break;
					default:
						continue;
					}
					if (mr == null)
						continue;
					var xmldoc = XmlDocLoader.LoadDocumentation(mr.Module);
					if (xmldoc == null)
						continue;
					string? doc = xmldoc.GetDocumentation(XmlDocKeyProvider.GetKey(mr, stringBuilder));
					if (!string2.IsNullOrEmpty(doc))  {
						InsertXmlDocumentation(entity, doc);
					}
				}
			} catch (XmlException ex) {
				string[] msg = (" Exception while reading XmlDoc: " + ex).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				var insertionPoint = node.FirstChild;
				for (int i = 0; i < msg.Length; i++)
					node.InsertChildBefore(insertionPoint, new Comment(msg[i], CommentType.Documentation), Roles.Comment);
			}
		}

		void InsertXmlDocumentation(AstNode node, string doc) {
			foreach (var info in new XmlDocLine(doc)) {
				stringBuilder.Clear();
				if (info is not null) {
					stringBuilder.Append(' ');
					info.Value.WriteTo(stringBuilder);
				}
				node.Parent.InsertChildBefore(node, new Comment(stringBuilder.ToString(), CommentType.Documentation), Roles.Comment);
			}
		}
	}
}
