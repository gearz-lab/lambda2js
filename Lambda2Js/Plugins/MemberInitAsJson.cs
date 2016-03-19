using System;
using System.Linq;
using System.Linq.Expressions;
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
                throw new ArgumentException("Argument is empty collection", nameof(newObjectTypes));

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

        public override void ConvertToJavascript(JavascriptConversionContext context)
        {
            var initExpr = context.Node as MemberInitExpression;
            if (initExpr == null)
                return;
            var typeOk1 = this.NewObjectTypes?.Contains(initExpr.Type) ?? false;
            var typeOk2 = this.TypePredicate?.Invoke(initExpr.Type) ?? false;
            var typeOk3 = this.NewObjectTypes == null && this.TypePredicate == null;
            if (!typeOk1 && !typeOk2 && !typeOk3)
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