$$
\begin{align}

    [\text{prog}] &\to [\text{stmt}]^* \\

    [\text{stmt}] &\to \begin{cases} 
                        \text{exit} \space [\text{expr}]; \\
                        \text{write} \space [\text{string}]; \\
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
                        \text{int\_lit} \\
                        \text{ident} \\
                        ([\text{expr}]) \\
                        [\text{pointer}] \\
                        *[\text{pointer}] \\
                        \text{'}char\text{'} \\
                        \text{"}string\text{"} \\
                        \end{cases} \\
    
    [\text{pointer}] &\to \begin{cases}
                        \text{\&ident}
                        \end{cases}

\end{align}
$$