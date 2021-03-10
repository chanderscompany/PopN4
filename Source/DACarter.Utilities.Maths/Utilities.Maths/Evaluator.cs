using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DACarter.Utilities.Maths {

	/// <summary>
	/// Evaluator class
	/// Evaluates a mathematical expression string and 
	///		calculates the numerical result
	///	Derived from http://www.codeproject.com/KB/cs/math_expressionsevaluator.aspx
	///		Copyright 2009 by HAKGERSoft  www.hakger.org
	///	Modified by DAC 2009Sept08
	///		0) Use period instead of comma for decimal point.
	///		1) Allow numbers that begin with decimal point, e.g. .1 as well as 0.1
	///		2) Throw error if illegal token is in expression string
	///		3) Correct function associativity so that func(x)*y 
	///			is interpreted as (func(x))*y NOT func(x*y)
	///		4) Modified to be static class.
	///	
	///	USEAGE: Only public method is Calculate():
	///		double result = Evaluator.Calculate("2.0+sin(.2)/3.1^2");
	///		
	/// </summary>
    public static class Evaluator {

		/*
		public Evaluator() {
        }
		*/

        public static double Calculate(string input) {
            Validate(input,x => !string.IsNullOrEmpty(x),"input cannot be empty");

            string expression=FormatExpression(input);
            Queue<Token> postfix=GetPostFix(expression);
            return ProcessPostfix(postfix);
        }

        private static void Validate<T>(T e,Func<T,bool> validator,string message){
            if(!validator(e))
                throw new ArgumentException(message);
        }

        private static Queue<Token> GetPostFix(string input) {
            Queue<Token> output=new Queue<Token>();
            Stack<Token> stack=new Stack<Token>();
            int position=0;
            while(position<input.Length) {
                Token token=GetNextToken(ref position,input);
				if (token == null) {
					// modified by dac to throw error
					//	if unknown tokens in string
					throw new ArgumentException();
					//break;
				}
                if(token is NumberBase)
                    output.Enqueue(token);
                else if(token is FunctionBase)
                    stack.Push(token);
                else if(token is LeftBracket)
                    stack.Push(token);
                else if(token is RightBracket) {
                    while(true) {
                        Token taken=stack.Pop();
                        if(!(taken is LeftBracket))
                            output.Enqueue(taken);
                        else {
							// added dac
							Token top = stack.Peek();
							if (top is FunctionBase) {
								stack.Pop();
								output.Enqueue(top);
							}
							// end dac
                            break;
                        }
                    }
                } else if(token is OperatorBase) {
                    if(stack.Count>0) {
                        Token top=stack.Peek();
                        bool nested=true;
                        while(nested) {
                            if(top==null || !(top is OperatorBase))
                                break;
                            OperatorBase o1=(OperatorBase)token;
                            OperatorBase o2=(OperatorBase)top;
                            if(o1.Associativity==Associativity.Left && (o2.Precedence>=o1.Precedence))
                                output.Enqueue(stack.Pop());
                            else if(o2.Associativity==Associativity.Right && (o2.Precedence>o1.Precedence))
                                output.Enqueue(stack.Pop());
                            else
                                nested=false;
                            top=(stack.Count>0)?stack.Peek():null;
                        }
                    }
                    stack.Push(token);
                }
            }
            while(stack.Count>0) {
                Token next=stack.Pop();
                if(next is LeftBracket || next is RightBracket) {
                    throw new ArgumentException();
                }
                output.Enqueue(next);
            }
            return output;
        }

        private static double ProcessPostfix(Queue<Token> postfix){
            Stack<Token> stack=new Stack<Token>();
            Token token=null;
            while(postfix.Count>0) {
                token=postfix.Dequeue();
                if(token is NumberBase)
                    stack.Push(token);
                else if(token is OperatorBase){
                    NumberBase right=(NumberBase)stack.Pop();
                    NumberBase left=(NumberBase)stack.Pop();
                    double value=((OperatorBase)token).Calculate(left.Value,right.Value);
                    stack.Push(new Number(value));
                }
                else if(token is FunctionBase){
                    NumberBase arg=(NumberBase)stack.Pop();
                    double value=((FunctionBase)token).Calculate(arg.Value);
                    stack.Push(new Number(value));
                }
            }
            double toret=((NumberBase)stack.Pop()).Value;
            if(stack.Count!=0)
                throw new ArgumentException();
            return toret;
        }

        private static Token GetNextToken(ref int position,string input) {
            Token toret=null;
            Type found=null;
            string rest=input.Substring(position);
            int count=0;
            int pos=0;
            while(count++<rest.Length) {
                string cand=rest.Substring(0,count);
                Token latest=TokenFactory.GetToken(cand);
                if(latest!=null) {
                    //if(found!=null && latest.GetType()!=found)
                        //break;
                    found=latest.GetType();
                    toret=latest;
                    pos=count;
                }
                else {
                    //break;
                }
            }
            if(toret!=null)
                position+=pos;
            return toret;
        }

        private static string FormatExpression(string input) {
            string toret=input;
            toret=toret.Replace(" ",string.Empty);
            //toret=toret.Replace(".",",");
            toret=Regex.Replace(toret,@"^\-",@"0-");
            toret=Regex.Replace(toret,@"\(\-",@"(0-");
            return toret;
        }
        
 

    }
}
