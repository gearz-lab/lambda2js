using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Lambda2Js
{
    public class MemberInitAsJson : JavascriptConversionExtension
    {
        public Predicate<Type> TypePredicate { get; }

        public Type[] NewObjectTypes { get; }

        public static readonly MemberInitAsJson ForAllTypes = new MemberInitAsJson();

        public MemberInitAsJson([NotNull] params Type[] newObjectTypes)
        {
            if (newObjectTypes == null)
                throw new ArgumentNullException(nameof(newObjectTypes));
            if (newObjectTypes.Length == 0)
                throw new ArgumentException("Argument is empty collection", nameof(newObjectTypes));

            this.NewObjectTypes = newObjectTypes;
        }

        private MemberInitAsJson()
        {
        }

        public MemberInitAsJson([NotNull] Predicate<Type> typePredicate)
        {
            if (typePredicate == null)
                throw new ArgumentNullException(nameof(typePredicate));
            this.TypePredicate = typePredicate;
        }

        public override void ConvertToJavascript(JavascriptConversionContext context)
        {
            var initExpr = context.Node as MemberInitExpression;
            if (initExpr == null)
                return;
            if (!this.NewObjectTypes.Contains(initExpr.Type))
                return;
            if (initExpr.NewExpression.Arguments.Count > 0)
                return;
            if (initExpr.Bindings.Any(mb => mb.BindingType != MemberBindingType.Assignment))
                return;

            var writer = context.GetWriter();
            using (writer.Operation(0))
            {
                writer.Write('{');

                var posStart = writer.Length;
                foreach (var assignExpr in initExpr.Bindings.Cast<MemberAssignment>())
                {
                    if (writer.Length > posStart)
                        writer.Write(',');

                    if (Regex.IsMatch(assignExpr.Member.Name, @"^\w[\d\w]*$"))
                        writer.Write(assignExpr.Member.Name);
                    else
                        writer.WriteLiteral(assignExpr.Member.Name);

                    writer.Write(':');
                    context.Visitor.Visit(assignExpr.Expression);
                }

                writer.Write('}');
            }
        }
    }
}