/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Apache License, Version 2.0, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

#if FEATURE_CORE_DLR
using MSAst = System.Linq.Expressions;
#else
using MSAst = Microsoft.Scripting.Ast;
#endif

using System;
using System.Diagnostics;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace IronPython.Compiler.Ast {
    using Ast = MSAst.Expression;
    using IronPython.Runtime.Operations;
    using System.Collections;

    // New in Pep380 for Python 3.3. Yield is an iterable expression with a return value.
    //    x = yield z
    // The return value (x) is provided by calling Generator.Send()
    public class YieldFromExpression : Expression {
        private readonly Expression _expression;

        public YieldFromExpression(Expression expression) {
            _expression = expression;
        }

        public Expression Expression {
            get { return _expression; }
        }

        public override MSAst.Expression Reduce() {
            // yield from basically reduces to:
            //
            // iterValue = _expression;
            // object result;
            // IEnumerator iterable = null;
            // while (true) {
            //  if (PythonOps.YieldFromNext(outerGenerator, iterValue, out result, ref iterable)) {
            //      yield return result;
            //  } else {
            //      expressionResult = LightExceptions.CheckAndThrow(result);
            //      break;
            //  }    
            //
            // and expressionResult represents the ending value (but is just passed w/ as the goto value in the ET below)

            var iterValue = Expression.Parameter(typeof(object), "iterValue");
            var result = Expression.Parameter(typeof(object), "result");
            var iterableTemp = Expression.Parameter(typeof(IEnumerator), "iterableTemp");
            var label = Expression.Label(typeof(object), "yieldFromBreak");

            return Ast.Block(
                new[] { iterValue, result, iterableTemp },
                Expression.Assign(
                    iterValue,
                    AstUtils.Convert(_expression, typeof(object))
                ),                
                Expression.Loop(
                    Expression.Condition(
                        Expression.Call(
                            AstMethods.YieldFromNext,
                            GeneratorRewriter._generatorParam,
                            iterValue,
                            result,
                            iterableTemp
                        ),
                        // true, we've yielded another value
                        Expression.Block(           
                            AstUtils.YieldReturn(
                                GeneratorLabel,
                                result
                            ),                            
                            Expression.Default(typeof(object))
                        ),
                        // false, we've returned a value
                        Expression.Goto(label, LightExceptions.CheckAndThrow(result), typeof(object)) 
                    )
                ),
                Expression.Label(label, Expression.Default(typeof(object)))                
            );
        }

        public override void Walk(PythonWalker walker) {
            if (walker.Walk(this)) {
                if (_expression != null) {
                    _expression.Walk(walker);
                }
            }
            walker.PostWalk(this);
        }

        internal override string CheckAssign() {
            return "can't assign to yield from expression";
        }

        internal override string CheckAugmentedAssign() {
            return CheckAssign();
        }

        public override string NodeName {
            get {
                return "yield from expression";
            }
        }
    }
}
