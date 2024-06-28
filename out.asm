global _start
_start:
    ; Define finalErrorCode variable of type SignedInteger64
    mov rax, 5 ; IntLit expression
    push rax
    mov rax, 64 ; IntLit expression
    push rax
    pop rbx ; Binary expression
    pop rax
    add rax, rbx
    push rax
    mov rax, 2 ; IntLit expression
    push rax
    pop rbx ; Binary expression
    pop rax
    idiv rbx
    push rax
    mov rax, 3 ; IntLit expression
    push rax
    pop rbx ; Binary expression
    pop rax
    imul rbx
    push rax
    ; Define actualErrorCode variable of type UnsignedInteger64
    push QWORD [rsp + 0] ; finalErrorCode variable
    ; Start scope
    ; Define x variable of type UnsignedInteger64
    mov rax, 15 ; IntLit expression
    push rax
    ; Define y variable of type SignedInteger64
    push QWORD [rsp + 0] ; x variable
    mov rax, 16 ; IntLit expression
    push rax
    pop rbx ; Binary expression
    pop rax
    sub rax, rbx
    push rax
    add rsp, 16 ; End scope with 2 variable(s)
    ; Assign actualErrorCode
    push QWORD [rsp + 0] ; actualErrorCode variable
    mov rax, 2 ; IntLit expression
    push rax
    pop rbx ; Binary expression
    pop rax
    mul rbx
    push rax
    pop rax
    mov QWORD [rsp + 0], rax
    push QWORD [rsp + 0] ; actualErrorCode variable
    mov rax, 60 ; exit
    pop rdi
    syscall
    mov rax, 60 ; End of program
    mov rdi, 0
    syscall
