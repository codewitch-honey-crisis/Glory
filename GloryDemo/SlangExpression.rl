verbatimIdentifier<id=68, terminal>= '@(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])*'
outKeyword<id=69, terminal>= 'out'
refKeyword<id=70, terminal>= 'ref'
typeOf<id=71, terminal>= 'typeof'
defaultOf<id=72, terminal>= 'default'
newKeyword<id=73, terminal>= 'new'
globalKeyword<id=74, terminal>= 'global'
stringType<id=75, terminal>= 'string'
boolType<id=76, terminal>= 'bool'
charType<id=77, terminal>= 'char'
floatType<id=78, terminal>= 'float'
doubleType<id=79, terminal>= 'double'
decimalType<id=80, terminal>= 'decimal'
sbyteType<id=81, terminal>= 'sbyte'
byteType<id=82, terminal>= 'byte'
shortType<id=83, terminal>= 'short'
ushortType<id=84, terminal>= 'ushort'
intType<id=85, terminal>= 'int'
uintType<id=86, terminal>= 'uint'
longType<id=87, terminal>= 'long'
ulongType<id=88, terminal>= 'ulong'
objectType<id=89, terminal>= 'object'
boolLiteral<id=90, terminal>= 'true|false'
nullLiteral<id=91, terminal>= 'null'
thisRef<id=92, terminal>= 'this'
baseRef<id=93, terminal>= 'base'
verbatimStringLiteral<id=94, terminal>= '@"([^"|""])*"'
stringLiteral<id=96, terminal>= '"([^\\"\'\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})*"'
characterLiteral<id=97, terminal>= '[\u0027]([^\\"\'\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})[\u0027]'
lte<id=98, terminal>= '\<='
lt<id=99, terminal>= '\<'
gte<id=100, terminal>= '\>='
gt<id=101, terminal>= '\>'
eqEq<id=102, terminal>= '=='
notEq<id=103, terminal>= '!='
eq<id=104, terminal>= '='
inc<id=105, terminal>= '\+\+'
addAssign<id=106, terminal>= '\+='
add<id=107, terminal>= '\+'
dec<id=108, terminal>= '\-\-'
subAssign<id=109, terminal>= '\-='
sub<id=110, terminal>= '\-'
mulAssign<id=111, terminal>= '\*='
mul<id=112, terminal>= '\*'
divAssign<id=113, terminal>= '/='
div<id=114, terminal>= '/'
modAssign<id=115, terminal>= '%='
mod<id=116, terminal>= '%'
and<id=117, terminal>= '&&'
bitwiseAndAssign<id=118, terminal>= '&='
bitwiseAnd<id=119, terminal>= '&'
or<id=120, terminal>= '\|\|'
bitwiseOrAssign<id=121, terminal>= '\|='
bitwiseOr<id=122, terminal>= '\|'
not<id=123, terminal>= '!'
lbracket<id=124, terminal>= '\['
rbracket<id=125, terminal>= '\]'
lparen<id=126, terminal>= '\('
rparen<id=127, terminal>= '\)'
lbrace<id=128, terminal>= '\{'
rbrace<id=129, terminal>= '\}'
comma<id=130, terminal>= ','
colonColon<id=131, terminal>= '::'
dot<id=132, terminal>= '\.'
whitespace<id=135, hidden>= '[ \t\r\n\v\f]+'
integerLiteral<id=133, priority= -50>= '(0x[0-9A-Fa-f]{1,16}|([0-9]+))([Uu][Ll]?|[Ll][Uu]?)?'
floatLiteral<id=134, priority= -51>= '(([0-9]+)(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?[DdMmFf]?)|((\.[0-9]+)([Ee][\+\-]?[0-9]+)?[DdMmFf]?)'
identifier<id=95, priority= -100>= '(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])*'

