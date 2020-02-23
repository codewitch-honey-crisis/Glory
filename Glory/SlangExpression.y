%token dot lparen rparen lbracket rbracket outKeyword refKeyword boolType charType stringType floatType doubleType decimalType sbyteType byteType shortType ushortType intType uintType longType ulongType objectType globalKeyword colonColon lt gt comma newKeyword lbrace rbrace add sub not inc dec verbatimStringLiteral characterLiteral integerLiteral floatLiteral stringLiteral boolLiteral nullLiteral typeOf defaultOf thisRef baseRef verbatimIdentifier identifier lte gte eqEq notEq bitwiseAnd bitwiseOr and or eq addAssign subAssign mulAssign divAssign modAssign bitwiseAndAssign bitwiseOrAssign mul div mod whitespace
%%
Expression : AssignExpression;
RelationalExpression : TermExpression RelationalExpressionList
	| TermExpression;
EqualityExpression : RelationalExpression EqualityExpressionList
	| RelationalExpression;
BitwiseAndExpression : EqualityExpression BitwiseAndExpressionList
	| EqualityExpression;
BitwiseOrExpression : BitwiseAndExpression BitwiseOrExpressionList
	| BitwiseAndExpression;
AndExpression : BitwiseOrExpression AndExpressionList
	| BitwiseOrExpression;
OrExpression : AndExpression OrExpressionList
	| AndExpression;
AssignExpression : OrExpression AssignExpressionList
	| OrExpression;
TermExpression : FactorExpression TermExpressionList
	| FactorExpression;
FactorExpression : UnaryExpression FactorExpressionList
	| UnaryExpression;
ArgList : Expression ArgListList
	| Expression;
MethodArgList : MethodArg MethodArgListList
	| MethodArg
	|;
MemberFieldRef : dot Identifier;
MemberInvokeRef : lparen MethodArgList rparen;
MemberIndexerRef : lbracket ArgList rbracket;
MemberAnyRef : MemberFieldRef
	| MemberInvokeRef
	| MemberIndexerRef;
MethodArg : outKeyword Expression
	| refKeyword Expression
	| Expression;
IntrinsicType : boolType
	| charType
	| stringType
	| floatType
	| doubleType
	| decimalType
	| sbyteType
	| byteType
	| shortType
	| ushortType
	| intType
	| uintType
	| longType
	| ulongType
	| objectType;
TypeBase : globalKeyword colonColon Identifier TypeBaseList
	| globalKeyword colonColon Identifier
	| Identifier TypeBaseList2
	| Identifier
	| IntrinsicType;
Type : TypeElement TypeArraySpecList
	| TypeElement;
TypeElement : TypeBase TypeGenericPart
	| TypeBase;
TypeGenericPart : lt Type TypeGenericPartList gt
	| lt Type gt
	| lt gt;
TypeArraySpec : lbracket TypeArraySpecRankList rbracket
	| lbracket rbracket;
TypeArraySpecRank : comma;
CastExpression : lparen Type rparen UnaryExpression;
ArraySpec : lbracket Expression rbracket ArraySpecList
	| lbracket Expression rbracket;
NewExpression : newKeyword TypeElement NewObjectPart
	| newKeyword TypeElement NewArrayPart;
NewObjectPart : lparen ArgList rparen
	| lparen rparen;
NewArrayPart : ArraySpec;
ArrayInitializer : lbrace Expression ArrayInitializerList rbrace
	| lbrace Expression rbrace
	| lbrace rbrace;
SubExpression : lparen Expression rparen;
UnaryExpression : add UnaryExpression
	| sub UnaryExpression
	| not UnaryExpression
	| inc UnaryExpression
	| dec UnaryExpression
	| CastExpression
	| PrimaryExpression;
PrimaryExpression : verbatimStringLiteral MemberAnyRefList
	| verbatimStringLiteral
	| characterLiteral MemberAnyRefList2
	| characterLiteral
	| integerLiteral MemberAnyRefList3
	| integerLiteral
	| floatLiteral MemberAnyRefList4
	| floatLiteral
	| stringLiteral MemberAnyRefList5
	| stringLiteral
	| boolLiteral MemberAnyRefList6
	| boolLiteral
	| nullLiteral
	| SubExpression MemberAnyRefList7
	| SubExpression
	| typeOf lparen Type rparen MemberAnyRefList8
	| typeOf lparen Type rparen
	| defaultOf lparen Type rparen MemberAnyRefList9
	| defaultOf lparen Type rparen
	| NewExpression MemberAnyRefList10
	| NewExpression
	| thisRef MemberAnyRefList11
	| thisRef
	| baseRef MemberAnyRefList12
	| baseRef
	| Identifier MemberAnyRefList13
	| Identifier
	| Type MemberFieldRef MemberAnyRefList14
	| Type MemberFieldRef;
Identifier : verbatimIdentifier
	| identifier;
RelationalExpressionList : RelationalExpressionList lt TermExpression
	| RelationalExpressionList lte TermExpression
	| RelationalExpressionList gt TermExpression
	| RelationalExpressionList gte TermExpression
	| lt TermExpression
	| lte TermExpression
	| gt TermExpression
	| gte TermExpression;
EqualityExpressionList : EqualityExpressionList eqEq RelationalExpression
	| EqualityExpressionList notEq RelationalExpression
	| eqEq RelationalExpression
	| notEq RelationalExpression;
BitwiseAndExpressionList : BitwiseAndExpressionList bitwiseAnd EqualityExpression
	| bitwiseAnd EqualityExpression;
BitwiseOrExpressionList : BitwiseOrExpressionList bitwiseOr BitwiseAndExpression
	| bitwiseOr BitwiseAndExpression;
AndExpressionList : AndExpressionList and BitwiseOrExpression
	| and BitwiseOrExpression;
OrExpressionList : OrExpressionList or AndExpression
	| or AndExpression;
AssignExpressionList : AssignExpressionList eq OrExpression
	| AssignExpressionList addAssign OrExpression
	| AssignExpressionList subAssign OrExpression
	| AssignExpressionList mulAssign OrExpression
	| AssignExpressionList divAssign OrExpression
	| AssignExpressionList modAssign OrExpression
	| AssignExpressionList bitwiseAndAssign OrExpression
	| AssignExpressionList bitwiseOrAssign OrExpression
	| eq OrExpression
	| addAssign OrExpression
	| subAssign OrExpression
	| mulAssign OrExpression
	| divAssign OrExpression
	| modAssign OrExpression
	| bitwiseAndAssign OrExpression
	| bitwiseOrAssign OrExpression;
TermExpressionList : TermExpressionList add FactorExpression
	| TermExpressionList sub FactorExpression
	| add FactorExpression
	| sub FactorExpression;
FactorExpressionList : FactorExpressionList mul UnaryExpression
	| FactorExpressionList div UnaryExpression
	| FactorExpressionList mod UnaryExpression
	| mul UnaryExpression
	| div UnaryExpression
	| mod UnaryExpression;
ArgListList : ArgListList comma Expression
	| comma Expression;
MethodArgListList : MethodArgListList comma MethodArg
	| comma MethodArg;
TypeBaseList : TypeBaseList dot Identifier
	| dot Identifier;
TypeBaseList2 : TypeBaseList2 dot Identifier
	| dot Identifier;
TypeArraySpecList : TypeArraySpecList TypeArraySpec
	| TypeArraySpec;
TypeGenericPartList : TypeGenericPartList comma Type
	| comma Type;
TypeArraySpecRankList : TypeArraySpecRankList TypeArraySpecRank
	| TypeArraySpecRank;
ArraySpecList2 : ArraySpecList2 comma
	| comma;
ArraySpecList3 : ArraySpecList3 comma
	| comma;
ArraySpecList : ArraySpecList lbracket ArraySpecList2 rbracket
	| ArraySpecList lbracket rbracket
	| lbracket ArraySpecList3 rbracket
	| lbracket rbracket;
ArrayInitializerList : ArrayInitializerList comma Expression
	| comma Expression;
MemberAnyRefList : MemberAnyRefList MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList2 : MemberAnyRefList2 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList3 : MemberAnyRefList3 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList4 : MemberAnyRefList4 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList5 : MemberAnyRefList5 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList6 : MemberAnyRefList6 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList7 : MemberAnyRefList7 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList8 : MemberAnyRefList8 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList9 : MemberAnyRefList9 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList10 : MemberAnyRefList10 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList11 : MemberAnyRefList11 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList12 : MemberAnyRefList12 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList13 : MemberAnyRefList13 MemberAnyRef
	| MemberAnyRef;
MemberAnyRefList14 : MemberAnyRefList14 MemberAnyRef
	| MemberAnyRef;
