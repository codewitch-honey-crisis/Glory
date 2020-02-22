verbatimIdentifier<id=74, terminal>= '@(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])*'
outKeyword<id=75, terminal>= 'out'
refKeyword<id=76, terminal>= 'ref'
typeOf<id=77, terminal>= 'typeof'
defaultOf<id=78, terminal>= 'default'
newKeyword<id=79, terminal>= 'new'
globalKeyword<id=80, terminal>= 'global'
stringType<id=81, terminal>= 'string'
boolType<id=82, terminal>= 'bool'
charType<id=83, terminal>= 'char'
floatType<id=84, terminal>= 'float'
doubleType<id=85, terminal>= 'double'
decimalType<id=86, terminal>= 'decimal'
sbyteType<id=87, terminal>= 'sbyte'
byteType<id=88, terminal>= 'byte'
shortType<id=89, terminal>= 'short'
ushortType<id=90, terminal>= 'ushort'
intType<id=91, terminal>= 'int'
uintType<id=92, terminal>= 'uint'
longType<id=93, terminal>= 'long'
ulongType<id=94, terminal>= 'ulong'
objectType<id=95, terminal>= 'object'
boolLiteral<id=96, terminal>= 'true|false'
nullLiteral<id=97, terminal>= 'null'
thisRef<id=98, terminal>= 'this'
baseRef<id=99, terminal>= 'base'
verbatimStringLiteral<id=100, terminal>= '@"([^"|""])*"'
stringLiteral<id=102, terminal>= '"([^\\"\'\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})*"'
characterLiteral<id=103, terminal>= '[\u0027]([^\\"\'\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})[\u0027]'
lte<id=104, terminal>= '\<='
lt<id=105, terminal>= '\<'
gte<id=106, terminal>= '\>='
gt<id=107, terminal>= '\>'
eqEq<id=108, terminal>= '=='
notEq<id=109, terminal>= '!='
eq<id=110, terminal>= '='
inc<id=111, terminal>= '\+\+'
addAssign<id=112, terminal>= '\+='
add<id=113, terminal>= '\+'
dec<id=114, terminal>= '\-\-'
subAssign<id=115, terminal>= '\-='
sub<id=116, terminal>= '\-'
mulAssign<id=117, terminal>= '\*='
mul<id=118, terminal>= '\*'
divAssign<id=119, terminal>= '/='
div<id=120, terminal>= '/'
modAssign<id=121, terminal>= '%='
mod<id=122, terminal>= '%'
and<id=123, terminal>= '&&'
bitwiseAndAssign<id=124, terminal>= '&='
bitwiseAnd<id=125, terminal>= '&'
or<id=126, terminal>= '\|\|'
bitwiseOrAssign<id=127, terminal>= '\|='
bitwiseOr<id=128, terminal>= '\|'
not<id=129, terminal>= '!'
lbracket<id=130, terminal>= '\['
rbracket<id=131, terminal>= '\]'
lparen<id=132, terminal>= '\('
rparen<id=133, terminal>= '\)'
lbrace<id=134, terminal>= '\{'
rbrace<id=135, terminal>= '\}'
comma<id=136, terminal>= ','
colonColon<id=137, terminal>= '::'
dot<id=138, terminal>= '\.'
whitespace<id=141, hidden>= '[ \t\r\n\v\f]+'
integerLiteral<id=139, priority= -50>= '(0x[0-9A-Fa-f]{1,16}|([0-9]+))([Uu][Ll]?|[Ll][Uu]?)?'
floatLiteral<id=140, priority= -51>= '(([0-9]+)(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?[DdMmFf]?)|((\.[0-9]+)([Ee][\+\-]?[0-9]+)?[DdMmFf]?)'
identifier<id=101, priority= -100>= '(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])*'

