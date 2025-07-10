# ⚠️ Atenção: Persistência de RenkoBricks e Pipeline de Cálculo/Predição

Este documento serve como alerta e orientação para desenvolvedores que utilizam o fluxo de geração de RenkoBricks com persistência em disco e execução de cálculos de features e predição.

---

## 📦 Persistência no Disco

Na classe `NelogicaRenkoGenerator`, o método `CreateAndAddBrick`:

- Adiciona o novo `RenkoBrick` ao buffer em memória (`_bricks` e `_renkoBuffer`).
- Persiste o brick no arquivo **proto-bin** através de `AppendBrickToFile`.
- Dispara o evento `OnCloseBrick`.

A persistência com `AppendBrickToFile` está atualmente **síncrona**, o que garante:

✅ Dados consistentes no arquivo antes de qualquer outra operação.

---

## 🚨 Cálculo/Predição: Cuidados com Bloqueio

Se o cálculo das features e a predição (via modelo) **for chamado dentro do **``, ele:

❌ **Será executado somente após o término da persistência no disco.**

Isso significa que o fluxo completo estará bloqueado até o final do cálculo/predição.

### 🔥 Como evitar bloqueios

Para garantir que o cálculo e a predição ocorram **em paralelo com a persistência**, é necessário desacoplar as responsabilidades:

✅ Chame a pipeline de cálculo/predição **fora do **``.

✅ Ou, se for imprescindível dentro do `OnCloseBrick`, use `Task.Run`:

```csharp
OnCloseBrick += brick =>
{
    _ = Task.Run(() =>
    {
        // Cálculo de features e predição aqui
    });
};
```

---

## 🏁 Resumo

| Cenário                                   | Persistência e Cálculo Simultâneos? |
| ----------------------------------------- | ----------------------------------- |
| Cálculo/Predição fora do `OnCloseBrick`   | ✅ Sim                               |
| Cálculo/Predição dentro do `OnCloseBrick` | ❌ Não (bloqueio)                    |
| Cálculo/Predição com `Task.Run`           | ✅ Sim                               |

---

## 📂 Onde salvar este documento

Salve este arquivo na pasta:

```
docs/renko_persistencia_e_pipeline.md
```

Assim ele ficará disponível para consulta por toda a equipe de desenvolvimento.

---

## 📌 Conclusão

⚠️ **Sempre avalie onde o cálculo de features e a predição estão sendo chamados.**

🔑 Para máxima performance e menor latência:

- **Persistência síncrona.**
- **Cálculo/Predição assíncronos.**

📝 Esta separação garante que o sistema continue recebendo novos trades sem bloqueios mesmo durante operações pesadas de cálculo e inferência.

