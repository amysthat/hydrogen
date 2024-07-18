$$
\begin{align}

    [\text{prog}] &\to [\text{stmt}]^* \\

    [\text{stmt}] &\to \begin{cases} 
                        \text{exit} \space [\text{expr}]; \\
                        \text{write} \space [\text{char*}]; \\
                        \text{ident} : [\text{varType}] = [\text{expr}]; \\
                        \text{ident} = [\text{expr}]; \\
                        [\text{scope}] \\
                        [\text{ifstmt}] \\
                        \end{cases} \\

    [\text{ifstmt}] &\to \begin{cases}
                        \text{if} \space [\text{expr}] \space [\text{scope}] \\
                        [\text{elif} \space [\text{expr}] \space  [\text{scope}]]^* \\
                        [\text{else} \space [\text{scope}]] ^\text{optional}
                        \end{cases} \\
    
    [\text{scope}] &\to \begin{cases}
                        \{[\text{stmt}]^*\} \\
                        \end{cases} \\

    [\text{expr}] &\to \begin{cases}
                        [\text{term}] \\
                        [\text{binExpr}] \\
                        [\text{varType}] \space \text{cast} \space [\text{expr}] \\
                        \end{cases} \\

    [\text{binExpr}] &\to \begin{cases}
                        [\text{term}] * [\text{expr}] & \text{prec} = 1 \\
                        [\text{term}]\space/\space[\text{expr}] & \text{prec} = 1 \\
                        [\text{term}] + [\text{expr}] & \text{prec} = 0 \\
                        [\text{term}] - [\text{expr}] & \text{prec} = 0 \\
                        \end{cases} \\
                        & ^\text{must have matching [varType]} \\
    
    [\text{term}] &\to \begin{cases}
                        [\text{integer}] \\
                        \text{ident} \\
                        ([\text{expr}]) \\
                        [\text{pointer}] \\
                        *[\text{pointer}] \\
                        \text{'}char\text{'} \\
                        \end{cases} \\

    [\text{integer}] &\to \begin{cases}
                        \text{int\_lit} \\
                        \end{cases} \\
    
    [\text{varType}] &\to \begin{cases}
                        \text{i64} \\
                        \text{u64} \\
                        \text{i16} \\
                        \text{u16} \\
                        \text{i32} \\
                        \text{u32} \\
                        \text{byte} \\
                        [\text{varType}]* \\
                        \text{char} \\
                        \end{cases} \\
    
    [\text{pointer}] &\to \begin{cases}
                        \text{\&ident}
                        \end{cases}

\end{align}
$$