using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DACarter.Utilities.Maths {

    public static class TokenFactory {
        static Dictionary<Func<string,bool>,Type> RegisteredTokens;

        static TokenFactory() {
            RegisteredTokens=new Dictionary<Func<string,bool>,Type>();

            //basic operators
            RegisterToken<Addition>(x => x=="+");
            RegisterToken<Subtraction>(x => x=="-");
            RegisterToken<Multiplication>(x => x=="*");
            RegisterToken<Division>(x => x=="/" || x==@"\");
            RegisterToken<Modulus>(x => x=="%");
            RegisterToken<Power>(x => x=="^");

            //Numbers
            RegisterToken<Pi>(x => Match(x,"pi"));
            RegisterToken<E>(x => x=="e");

            RegisterToken<Number>(x => {
				// one or more digits followed by
				// the group
				// (a period followed by
				// zero or more digits)
				// zero or one time
				//return Regex.Match(x, @"^\d+(\.\d*)?$").Success;
				

				// modified from above by dac to allow numbers beginning with "."
				// zero or more digits followed by
				// the group
				// (zero or one period followed by
				// zero or more digits)
				// zero or one time
				return Regex.Match(x, @"^\d*(\.?\d*)?$").Success;
			});

            //brackets
            RegisterToken<LeftBracket>(x => x=="(" || x=="[" || x=="{");
            RegisterToken<RightBracket>(x => x==")" || x=="]" || x=="}");

            //functions
			RegisterToken<Sinus>(x => Match(x, "sin"));
			RegisterToken<SinusD>(x => Match(x, "sind"));
			RegisterToken<Cosinus>(x => Match(x, "cos"));
			RegisterToken<CosinusD>(x => Match(x, "cosd"));
			RegisterToken<Tangent>(x => Match(x, "tan", "tg"));
			RegisterToken<TangentD>(x => Match(x, "tand"));
			RegisterToken<ABS>(x => Match(x, "abs"));
            RegisterToken<Sqrt>(x => Match(x,"sqrt"));
			RegisterToken<ArcTangent>(x => Match(x, "atan", "atg"));
			RegisterToken<ArcTangentD>(x => Match(x, "atand"));
			RegisterToken<ArcSinus>(x => Match(x, "asin"));
			RegisterToken<ArcSinusD>(x => Match(x, "asind"));
			RegisterToken<ArcCosinus>(x => Match(x, "acos"));
			RegisterToken<ArcCosinusD>(x => Match(x, "acosd"));
			RegisterToken<Ceil>(x => Match(x, "ceil"));
            RegisterToken<Floor>(x => Match(x,"floor"));
            RegisterToken<Round>(x => Match(x,"round"));
            RegisterToken<Exp>(x => Match(x,"Exp"));
            RegisterToken<Logarithm>(x => Match(x,"ln"));
            RegisterToken<Logarithm10>(x => Match(x,"log10", "log"));            
        }

        static bool Match(string cand,params string[] names) {
            foreach(string name in names) {
                if(name.Equals(cand,StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        static void RegisterToken<T>(Func<string,bool> MatchDel) where T:Token {
            RegisteredTokens[MatchDel]=typeof(T);
        }

        public static Token GetToken(string exact){
            Token toret=null;
            foreach(var kvp in RegisteredTokens){
                if(kvp.Key(exact)) {
                    toret=(Token)Activator.CreateInstance(kvp.Value,exact);
                    break;
                }
            }
            return toret;
        }


    }
}
