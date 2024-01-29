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
    valuecondition | contextcondition
    ;

contextcondition:
    notion
    ;

valuecondition:
    ('user' | notion) '?' value
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
    'end' '!' value
    ;

rejoinsentence:
    'rejoin' labelref
    ;

result:
    STRING
    ;

repsentence:
    (label | altlabel) '-"-'
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
    | checkpredicate
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

checkpredicate:
    '<check>' notion
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

value:
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

labelref:
    CHAR? NUMBER
    ;

NUMBER:
    [0-9]+
    ;

CHAR:
    [a-zA-Z]
    ;

STRING:
    CHAR+
    ;

WS:
    [ \t\r\n]+ -> skip
    ;
