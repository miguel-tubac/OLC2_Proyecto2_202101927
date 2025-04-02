grammar Language;

// ****************Analisis sitactico******************************

program: declaraciones*;

declaraciones
    : declararvar //Esto se realiza antes que los stmt 
    | stmt  //Este es un statemnt no declarativo
    | funcDcl
    | instStruct
; 

declararvar: 'var' ID tipos ('=' expr)? ';'                     # PrimeraDecl 
    | ID ':=' expr ';'                                          # SegundaDecl
    | ID ':=' '[' ']' tipos '{'(expr ','?)*'}' ';'              # SliceIniciali
    | 'var' ID '[' ']' tipos ';'                                # SliceNoIncial
    | ID ':=' '[' ']' '[' ']' tipos '{' filasMatriz '}' ';'     # Matris
    | ID ':=' ID '{' datos* '}'                                 # StructParam //****************************************************** */
    | ID '=' '[' ']' tipos '{'(expr ','?)*'}' ';'               # ReasignarSlice
;

datos: ID ':' expr (',' (ID ':' expr)?)*     //****************************************************** */
;

// Producción para las filas de la matriz (lista de listas)
filasMatriz
    : filaMatriz (',' filaMatriz?)* 
;

// Producción para una fila de la matriz
filaMatriz
    : '{' expr (',' expr)* '}'
;

//structs
instStruct
    : 'type' ID 'struct' '{' listaAtributos* '}'     //****************************************************** */
;

listaAtributos
    : ID (tipos|ID) ';'       //****************************************************** */
;


//Producion para las funciones
funcDcl: 'func' ID '(' params? ')' tipos? '{' declaraciones* '}'    # FuncDcl1
    | 'func' '(' ID ID ')' ID '(' params? ')' tipos? '{' declaraciones* '}' # FuncStruct
;

params: ID tipos? (',' ID tipos?)*
;


stmt: expr ';'                                          # ExpreStmt
    | 'fmt' '.' 'Println' '(' (expr ','?)* ')' ';'      # PrinStmt
    | '{' declaraciones* '}'                            # BloqueSente
    | asignacion   ';'                                  # SoloPasar
    | 'if' expr  stmt  ('else'  ( stmt ) )?             # Ifstat
    | 'switch' expr '{' instCase* instDefault? '}'      # InstrucSwitch
    | 'for' expr stmt                                   # WhileStmt
    | 'for' forInit expr ';' expr stmt                  # ForStmt
    | 'for' ID ',' ID ':=' 'range' ID stmt              # ForSlice
    | 'break' ';'                                       # BreakStmt
    | 'continue' ';'                                    # ContinueStmt
    | 'return' expr? ';'                                # ReturnStmt
;   

forInit: declararvar | expr ';'
;

//Casos del switch
instCase: 'case' expr ':' declaraciones*                      # InstrucCase
;
instDefault: 'default' ':' declaraciones*                     # InstrucDefault
;


asignacion: ID '+=' expr                        # Asig_Aumento
    | ID '-=' expr                              # Asig_Decre
    | ID '++'                                   # Aumento_Uno
    | ID '--'                                   # Decreme_Uno
    | 'strconv.Atoi' '('expr')'                 # FuncAtoi
    | 'strconv.ParseFloat' '('expr')'           # FuncParFloat
    | 'reflect.TypeOf' '('expr')'               # FuncTypeOf
    | 'slices.Index' '(' ID ',' expr')'         # FuncIndex
    | 'strings.Join' '('ID ',' expr')'          # FuncJoin
    | 'len' '('expr')'                          # FuncLen
    | 'append' '('ID',' expr')'                 # FuncAppend
    | ID '['expr']' '=' expr                    # AsignaList
    | ID '['expr']' '['expr']' '=' expr         # AsignaMatris
;


expr: '-' expr                                  # Negar
    | expr llamada+                             # LlamadaFuncio
    | op = '!' expr                             # Negacion
    | expr op = ('*' | '/' | '%') expr          # MulDiv
    | expr op = ('+' | '-') expr                # SumRes
    | expr op = ('>' | '<' | '>=' | '<=') expr  # Relacional
    | expr op = ('==' | '!=') expr              # Equalitys
    | expr op = '&&' expr                       # And
    | expr op = '||' expr                       # Or
    | booll                                     # Boolean
    | FLOAT                                     # Float
    | RUNE                                      # Rune
    | STRING                                    # String
    | INT                                       # Int
    | expr '=' expr                             # Assign 
    | 'nil'                                     # TipoNil
    | ID                                        # Identifaider
    | '(' expr ')'                              # Parens
    | asignacion                                # Asigna
    | ID '[' expr ']'                           # ObtenerPos
    | ID '[' expr ']' '[' expr ']'              # ObtenerMatris
;

//SOn las llamadas: z.c.f.d().f = das
llamada: '('args?')'                            # Llama
    | '.' ID                                    # Gets//****************************************************** */
;

args: expr (','expr)*;


// ****************Analisis lexico*********************************
//Esta es el area de los terminales
ID: [a-zA-Z_][a-zA-Z0-9_]*;
INT: [0-9]+;
FLOAT: [0-9]+'.'[0-9]+;
booll: 'true' | 'false';

STRING: '"' (ESC_SEQ | ~["\\])* '"';
RUNE: '\'' (ESC_SEQ | ~['\\]) '\'';

fragment ESC_SEQ: '\\' [btnr"\\]; 

WS: [ \t\r\n]+ -> skip;

LINEALCOMENT: '//' ~[\r\n]* -> channel(HIDDEN)  ;
BlockComment: '/*' .*? '*/' -> skip;

tipos: 'int' | 'float64' | 'bool' | 'string' | 'rune';
