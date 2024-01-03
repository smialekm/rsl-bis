grammar RslBis;

start: usecase+ dnotion* EOF
    ;

usecase:
    'Use case' name mainscenario scenarios?
    ;

ucconditions:
    '{' conditions '}'
    ;

conditions:
    condition (';' conditions)?
    ;

condition:
    STRING+ ('?' STRING+)?
    ;

mainscenario:
    'Main scenario' ucconditions? sentences endsentence
    ;

scenarios:
    scenario scenarios?
    ;

scenario:
    'Scenario' repsentence altsentences endsentence
    ;

sentences:
    sentence sentences?
    ;
	
altsentences:
    altsentence altsentences?
    ;

sentence:
    svosentence
    | condsentence
    ;

altsentence:
    altsvosentence
    | condsentence
    ;

condsentence:
    '[' conditions ']'
    ;

endsentence:
    '->' (resultsentence | rejoinsentence)
    ;

resultsentence:
    '{' result '}'
    ;

rejoinsentence:
    'rejoin' NUMBER
    ;

result:
    STRING
    ;

repsentence:
    label '-"-'
    ;

svosentence:
    label step
    ;

altsvosentence:
    altlabel step
    ;

step:
    userstep
    | systemstep
    ;

systemstep:
    'System' (tosystempredicate | toactorpredicate | invoke)
    ;

toactorpredicate:
    showpredicate
    | closepredicate
    ;

showpredicate:
    '<show>' notion
    ;

closepredicate:
     '<close>' notion
    ;

tosystempredicate:
    readpredicate
    | updatepredicate
    | deletepredicate
    | validatepredicate
    | executepredicate
    ;

readpredicate:
    '<read>' notion
    ;

updatepredicate:
    '<update>' notion
    ;

deletepredicate:
    '<delete>' notion
    ;

validatepredicate:
    '<validate>' notion
    ;

executepredicate:
    '<execute>' notion
    ;

userstep:
    actor (selectpredicate | enterpredicate | invoke)
    ;

selectpredicate:
    '<select>' notion
    ;

enterpredicate:
    '<enter>' notion
    ;

invoke:
    '<invoke>' name
    ;

dnotion:
    ('Frame' | 'Trigger' | 'Data') STRING+ ((':' STRING) | ('{' attributes '}'))
    ;

attributes:
    attribute (',' attributes)?
    ;

attribute:
    STRING+ ':' STRING
    ;

actor:
    STRING
    ;

notion:
    STRING+
    ;

name:
    STRING+
    ;

label:
    NUMBER ':'
    ;
    
altlabel:
    CHAR NUMBER ':'
    ;

NUMBER:
    [0-9]+
    ;

STRING:
    CHAR+
    ;

CHAR:
    [a-zA-Z]
    ;

WS:
    [ \t\r\n]+ -> skip
    ;
