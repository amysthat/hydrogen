global _start
_start:
    ; Define code variable
    mov rax, 0 ; IntLit expression
    push rax
    push QWORD [rsp + 0] ; code variable
    pop rax
    cmp rax, 0
    je label0
    ; Start scope
    ; Assign code
    mov rax, 0 ; IntLit expression
    push rax
    pop rax
    mov QWORD [rsp + 0], rax
    add rsp, 0 ; End scope with 0 variable(s)
    jmp label1
label0:
    ; Start scope
    ; Assign code
    mov rax, 10 ; IntLit expression
    push rax
    pop rax
    mov QWORD [rsp + 0], rax
    add rsp, 0 ; End scope with 0 variable(s)
label1:
    push QWORD [rsp + 0] ; code variable
    mov rax, 60 ; exit
    pop rdi
    syscall
    mov rax, 60 ; End of program
    mov rdi, 0
    syscall
