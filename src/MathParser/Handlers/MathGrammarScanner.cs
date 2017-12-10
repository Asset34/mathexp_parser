﻿using System;
using System.Collections.Generic;

using MathOptimizer.Parser.Handlers.TokenPredicates;
using MathOptimizer.Parser.Interfaces.Tokens;
using MathOptimizer.Parser.Interfaces.Predicates;

namespace MathOptimizer.Parser.Handlers
{
    //
    // Summary:
    //     Represents a part of the Parser which implement grammar analysis
    //     of the input math expression
    // 
    // Formal Grammar:
    //     <MathExp>  ::= <UnaryOp> <Operand> { <BinaryOp> <Operand> }*
    //     <Operand>  ::= <Variable> | <Constant > | <Number> | <Function> | '(' <MathExp> ')'
    //     <Function> ::= <FunctionName> '(' <MathExp> ')'
    class MathGrammarScanner : EmptyTokenVisitor
    {
        public MathGrammarScanner()
        {
            /* Build edges of FSM */

            // Terminal symbols
            ITokenPredicate variable      = new VariableTokenPredicate();
            ITokenPredicate constant      = new ConstantTokenPredicate();
            ITokenPredicate number        = new NumberTokenPredicate();
            ITokenPredicate binaryOp      = new BinaryOpTokenPredicate();
            ITokenPredicate unaryOp       = new UnaryOpTokenPredicate();
            ITokenPredicate functionName  = new FunctionNameTokenPredicate();
            ITokenPredicate lbracket      = new LBracketrTokenPredicate();
            ITokenPredicate rbracket      = new RBracketTokenPredicate();
            ITokenPredicate funcSeparator = new FuncSeparatorPredicate();

            DisjunctionTokenPredicate disjunctionPr = new DisjunctionTokenPredicate();

            // MathExp
            disjunctionPr.Predicates.Add(variable);
            disjunctionPr.Predicates.Add(constant);
            disjunctionPr.Predicates.Add(number);
            disjunctionPr.Predicates.Add(functionName);
            disjunctionPr.Predicates.Add(lbracket);
            disjunctionPr.Predicates.Add(unaryOp);

            edgesMathExp = disjunctionPr;
            disjunctionPr = new DisjunctionTokenPredicate();

            // Variable
            disjunctionPr.Predicates.Add(binaryOp);
            disjunctionPr.Predicates.Add(rbracket);
            disjunctionPr.Predicates.Add(funcSeparator);

            edgesVariable = disjunctionPr;
            disjunctionPr = new DisjunctionTokenPredicate();

            // UnaryOp
            disjunctionPr.Predicates.Add(variable);
            disjunctionPr.Predicates.Add(number);
            disjunctionPr.Predicates.Add(constant);
            disjunctionPr.Predicates.Add(functionName);
            disjunctionPr.Predicates.Add(lbracket);

            edgesUnaryOp = disjunctionPr;
            disjunctionPr = new DisjunctionTokenPredicate();

            // RBracket
            edgesrbracket = edgesVariable;

            // Number
            edgesNumber = edgesVariable;

            // Constant
            edgesConstant = edgesVariable;

            // BinaryOp
            edgesBinaryOp = edgesMathExp;

            // LBracket
            edgeslbracket = edgesMathExp;

            // FuncSeparator
            edgesFuncSeparator = edgesMathExp;

            // FunctionName
            edgesFunctionName = lbracket;

            /* Set start edges */
            edgesCurrent = edgesMathExp;
        }
        public void Scann(List<IToken> tokens)
        {
            // Reset handler
            Reset();

            // Handle tokens
            foreach (IToken t in tokens)
            {
                t.Accept(this);
            }

            CompareBracketCounters();
        }
        
        public override void Visit(IVariableToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgesVariable;
            }
            else
            {
                throwException(t);
            }
        }
        public override void Visit(IConstantToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgesConstant;
            }
            else
            {
                throwException(t);
            }
        }
        public override void Visit(IFunctionNameToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgesFunctionName;

                functionEnter(t);
            }
            else
            {
                throwException(t);
            }
        }
        public override void Visit(INumberToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgesNumber;
            }
            else
            {
                throwException(t);
            }
        }
        public override void Visit(ILBracketToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgeslbracket;

                bracketCounter++;
            }
            else
            {
                throwException(t);
            }
        }
        public override void Visit(IRBracketToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgesrbracket;

                bracketCounter--;

                if (bracketValues.Count !=0 && bracketCounter == bracketValues.Peek())
                {
                    functionLeave();
                }  
            }
            else
            {
                throwException(t);
            }
        }
        public override void Visit(IFuncSeparatorToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgesFuncSeparator;

                functionArgLeave();
            }
            else
            {
                throwException(t);
            }
        }
        public override void Visit(IBinaryOpToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgesBinaryOp;
            }
            else
            {
                throwException(t);
            }
        }
        public override void Visit(IUnaryOpToken t)
        {
            if (edgesCurrent.Execute(t))
            {
                edgesCurrent = edgesUnaryOp;
            }
            else
            {
                throwException(t);
            }
        }
        
        private void Reset()
        {
            // Reset function stack
            functions.Clear();

            // Reset counters
            argumentsCounters.Clear();
            maxAruments.Clear();
            bracketValues.Clear();
            bracketCounter = 0;

            // Reset start edge
            edgesCurrent = edgesMathExp;
        }
        private void throwException(IToken lastToken)
        {
            Exception ex = new Exception("Invalid expression");

            ex.Source = "MathSyntaxScanner";
            ex.Data.Add("Token", lastToken.ToString());

            throw ex;
        }
        private void CompareBracketCounters()
        {
            if (bracketCounter != 0)
            {
                Exception ex = new Exception("Invalid expression");

                ex.Source = "MathSyntaxScanner";

                if (bracketCounter < 0)
                {
                    ex.Data.Add("Missing", "(");
                }
                else
                {
                    ex.Data.Add("Missing", ")");
                }

                throw ex;
            }
        }

        private void functionEnter(IFunctionNameToken t)
        {
            functions.Push(t.ToString());
            argumentsCounters.Push(0);
            maxAruments.Push(Tables.FunctionsArgsNumberTable[t.ToString()]);
            bracketValues.Push(bracketCounter);
        }
        private void functionArgLeave()
        {
            argumentsCounters.Push(argumentsCounters.Pop() + 1);
        }
        private void functionLeave()
        {
            int args1 = maxAruments.Pop();
            int args2 = argumentsCounters.Pop() + 1;
            string funcName = functions.Pop();

            bracketValues.Pop();

            if (args1 != args2)
            {
                string msg;
                if (args2 < args1)
                {
                    msg = "Too few arguments in";
                }
                else
                {
                    msg = "Too many arguments in";
                }

                Exception ex = new Exception(msg);

                ex.Source = "MathSyntaxScanner";
                ex.Data.Add("Function", funcName);

                throw ex;
            }
        }
        
        /* Edges */
        private readonly ITokenPredicate edgesMathExp;
        private readonly ITokenPredicate edgesVariable;
        private readonly ITokenPredicate edgesConstant;
        private readonly ITokenPredicate edgesNumber;
        private readonly ITokenPredicate edgesBinaryOp;
        private readonly ITokenPredicate edgesUnaryOp;
        private readonly ITokenPredicate edgeslbracket;
        private readonly ITokenPredicate edgesrbracket;
        private readonly ITokenPredicate edgesFuncSeparator;
        private readonly ITokenPredicate edgesFunctionName;

        /* Current edge */
        private ITokenPredicate edgesCurrent;

        /* Current functions */
        private Stack<string> functions = new Stack<string>();

        /* Counters */
        private Stack<int> argumentsCounters = new Stack<int>();
        private Stack<int> maxAruments       = new Stack<int>();
        private Stack<int> bracketValues     = new Stack<int>();

        private int bracketCounter;
    }
}