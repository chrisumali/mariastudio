require.config({ paths: { vs: "/lib/monaco-editor/min/vs" } });

window.MonacoEnvironment = {
    getWorkerUrl: function () {
        const base = location.origin + "/lib/monaco-editor/min";
        return "data:text/javascript;charset=utf-8," + encodeURIComponent(
            `self.MonacoEnvironment = { baseUrl: "${base}/" };` +
            `importScripts("${base}/vs/base/worker/workerMain.js");`);
    }
};

require(["vs/editor/editor.main"], function () {
    const editor = monaco.editor.create(document.getElementById("editor"), {
        value: "SELECT 1;",
        language: "sql",
        theme: "vs-dark",
        fontSize: 14,
        minimap: { enabled: false },
        automaticLayout: true,
        wordWrap: "on",
        tabSize: 2
    });

    monaco.languages.registerCompletionItemProvider("sql", {
        triggerCharacters: [".", " "],
        provideCompletionItems: async function (model, position) {
            const word = model.getWordUntilPosition(position);
            const range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn
            };
            const response = await fetch("/Query/Complete?prefix=" + encodeURIComponent(word.word));
            const payload = await response.json();
            if (!response.ok || payload.error || !Array.isArray(payload))
                return { suggestions: [] };
            return {
                suggestions: payload.map(function (item) {
                    return {
                        label: item.name,
                        kind: item.kind === "table"
                            ? monaco.languages.CompletionItemKind.Class
                            : monaco.languages.CompletionItemKind.Field,
                        detail: item.detail,
                        insertText: item.name,
                        range: range
                    };
                })
            };
        }
    });

    async function run() {
        const status = document.getElementById("status");
        const button = document.getElementById("run");
        status.textContent = "Running…";
        button.disabled = true;
        try {
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            const response = await fetch("/Query/Execute", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": tokenInput.value
                },
                body: JSON.stringify({ sql: editor.getValue() })
            });
            const payload = await response.json();
            if (!response.ok) {
                status.textContent = payload.error || "Request failed.";
                renderResults({ results: [] });
                return;
            }
            const truncated = payload.results && payload.results.some(function (set) { return set.truncated; });
            status.textContent = payload.error
                ? payload.error
                : payload.elapsedMs + " ms, " + payload.rowsAffected + " affected" + (truncated ? ", results truncated at 500 rows" : "");
            renderResults(payload);
        } catch (error) {
            status.textContent = error.message;
        } finally {
            button.disabled = false;
        }
    }

    document.getElementById("run").addEventListener("click", run);
    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, run);
    loadTree(editor);
});

function renderResults(payload) {
    const host = document.getElementById("results");
    host.replaceChildren();
    if (payload.error || !payload.results)
        return;

    payload.results.forEach(function (set) {
        const table = document.createElement("table");
        table.className = "table table-sm table-striped";
        const head = document.createElement("tr");
        set.columns.forEach(function (name) {
            const th = document.createElement("th");
            th.textContent = name;
            head.appendChild(th);
        });
        const thead = document.createElement("thead");
        thead.appendChild(head);
        table.appendChild(thead);

        const tbody = document.createElement("tbody");
        set.rows.forEach(function (row) {
            const tr = document.createElement("tr");
            row.forEach(function (value) {
                const td = document.createElement("td");
                if (value === null) {
                    td.textContent = "NULL";
                    td.className = "null-value";
                } else {
                    td.textContent = String(value);
                }
                tr.appendChild(td);
            });
            tbody.appendChild(tr);
        });
        table.appendChild(tbody);
        host.appendChild(table);
    });
}

async function loadTree(editor) {
    const host = document.getElementById("schema");
    const status = document.getElementById("status");
    try {
        const response = await fetch("/Query/Tree");
        const payload = await response.json();
        if (!response.ok || payload.error || !Array.isArray(payload)) {
            status.textContent = payload.error || "Could not load schema.";
            return;
        }
        payload.forEach(function (node) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "schema-item";
            button.textContent = node.name;
            button.addEventListener("click", function () {
                const selection = editor.getSelection();
                editor.executeEdits("schema", [{
                    range: selection,
                    text: node.name,
                    forceMoveMarkers: true
                }]);
                editor.focus();
            });
            host.appendChild(button);
        });
    } catch (error) {
        status.textContent = error.message;
    }
}
