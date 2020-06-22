using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Lambda2Js
{
    public class MemberInitAsJson : JavascriptConversionExtension
    {
        [CanBeNull]
        public Predicate<Type> TypePredicate { get; }

        [CanBeNull]
        public Type[] NewObjectTypes { get; }

        public static readonly MemberInitAsJson ForAllTypes = new MemberInitAsJson();

        /// <summary>
        /// Initializes a new instance of <see cref="MemberInitAsJson"/>,
        /// so that member initializations of types in `newObjectTypes` are converted to JSON.
        /// </summary>
        public MemberInitAsJson([NotNull] params Type[] newObjectTypes)
        {
            if (newObjectTypes == null)
                throw new ArgumentNullException(nameof(newObjectTypes));
            if (newObjectTypes.Length == 0)
                throw new ArgumentException("Argument is empty collection. Maybe you are looking for `MemberInitAsJson.ForAllTypes`.", nameof(newObjectTypes));

            this.NewObjectTypes = newObjectTypes;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MemberInitAsJson"/>,
        /// so that member initializations of any types are converted to JSON.
        /// </summary>
        private MemberInitAsJson()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MemberInitAsJson"/>,
        /// so that member initializations of types that pass the `typePredicate` criteria are converted to JSON.
        /// </summary>
        public MemberInitAsJson([NotNull] Predicate<Type> typePredicate)
        {
            if (typePredicate == null)
                throw new ArgumentNullException(nameof(typePredicate));

            this.TypePredicate = typePredicate;
        }

        private bool IsAcceptableType(Type type)
        {
            var typeOk1 = NewObjectTypes?.Contains(type) ?? false;
            var typeOk2 = TypePredicate?.Invoke(type) ?? false;
            var typeOk3 = NewObjectTypes == null && TypePredicate == null;
            
            return typeOk1 || typeOk2 || typeOk3;
        }

        public override void ConvertToJavascript(JavascriptConversionContext context)
        {
            var initExpr = context.Node as MemberInitExpression;
            if (initExpr == null)
                return;

            if (!IsAcceptableType(initExpr.Type))
                return;

            context.PreventDefault();
            var writer = context.GetWriter();
            using (writer.Operation(0))
            {
                writer.Write('{');

                foreach (var binding in initExpr.Bindings)
                {
                    if(binding != initExpr.Bindings[0])
                        writer.Write(',');

                    WriteBinding(context, binding, writer);
                }

                writer.Write('}');
            }
        }

        /// <summary>
        /// Recursively callable WriteBinding() for MemberMemberBinding case
        /// </summary>
        /// <param name="context"></param>
        /// <param name="binding"></param>
        /// <param name="writer"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        private void WriteBinding(JavascriptConversionContext context, MemberBinding binding, JavascriptWriter writer)
        {
            var metadataProvider = context.Options.GetMetadataProvider();
            var meta = metadataProvider.GetMemberMetadata(binding.Member);
            var memberName = meta?.MemberName;
            Debug.Assert(!string.IsNullOrEmpty(memberName), "!string.IsNullOrEmpty(memberName)");
            if (Regex.IsMatch(memberName, @"^\w[\d\w]*$"))
                writer.Write(memberName);
            else
                writer.WriteLiteral(memberName);

            writer.Write(':');

            if (binding is MemberAssignment ma)
            {
                if (ma.Expression is NewExpression ne)
                {
                    if(!IsAcceptableType(ne.Type))
                        throw new InvalidOperationException($"Unable to initialize type: {ne.Type.FullName}");

                    if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(ne.Type.GetTypeInfo()))
                    {
                        writer.Write('{');

                        //Expressions don't support dictionary initializer syntax, so...
                        //Handle Dictionary(IEnumerable<KVP>) constructor - new Dictionary<string, Thing>(new[] { new KeyValuePair<string, Thing>("One", new Thing { Name = "Fred" }) }) 
                        if (ne.Arguments.Count == 1 && ne.Arguments[0] is NewArrayExpression nae)
                        {
                            foreach (var nie in nae.Expressions)
                            {
                                if (nie is NewExpression newItem)
                                {
                                    //Get KVP constructor args for Key and Value
                                    var key = newItem.Arguments[0];
                                    var value = newItem.Arguments[1];
                                    if(nie != nae.Expressions[0])
                                        writer.Write(',');
                                    context.Visitor.Visit(key);
                                    writer.Write(':');
                                    context.Visitor.Visit(value);
                                }
                                else
                                {
                                    throw new NotSupportedException($"Not supported for Dictionary item expression: {nie}");
                                }
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"Not supported for Dictionary constructor with {ne.Arguments.Count} arguments: {ne}");
                        }

                        writer.Write('}');
                    }
                    else
                    {
                        throw new NotSupportedException($"Not supported for non-dictionary constructor: {ne}");
                    }
                    
                }
                else
                {
                    context.Visitor.Visit(ma.Expression);
                }
                
            }
            else if (binding is MemberMemberBinding mmb)
            {
                //Nested object initializers: new Thing{ Nested = new NestedThing { Name="Fred" } }
                writer.Write('{');
                foreach (var mb in mmb.Bindings)
                {
                    if (mb != mmb.Bindings[0])
                        writer.Write(',');
                    WriteBinding(context, mb, writer);
                }
                writer.Write('}');
            }
            else if (binding is MemberListBinding mlb)
            {
                //List binding
                
                writer.Write('[');

                foreach (var initializer in mlb.Initializers)
                {
                    if (initializer != mlb.Initializers[0])
                        writer.Write(",");
                    context.Visitor.Visit(initializer.Arguments[0]);
                }

                writer.Write(']');
            }
            else
            {
                throw new NotSupportedException($"Unsupported: {binding.Member.Name} - {binding.BindingType} ({binding.GetType().FullName})");
            }
        }
    }
}