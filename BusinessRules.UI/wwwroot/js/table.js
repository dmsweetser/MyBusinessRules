// This function will serialize the table data into a comma-separated string and save it to the hidden input
function saveTableData(modelId, topLevelFieldId) {
    var data = "";
    var doSave = true;
    var table = document.getElementById(modelId + "-table");
    var rows = table.getElementsByTagName("tr");
    for (var i = 1; i < rows.length; i++) { // Skip the header row
        var cells = rows[i].getElementsByTagName("td");
        var key = cells[0].getElementsByTagName("input")[0].value;
        var value = cells[1].getElementsByTagName("input")[0].value;
        if (key !== "" && value !== "") {
            data += key + ":" + value + "|";
        } else if (!key || key === "" || !value || value === "") {
            doSave = false;
        }
    }
    data = data.slice(0, -1);
    document.getElementsByClassName(modelId + "-table-value")[0].value = data;
    if (doSave) document.getElementById("saveField_" + topLevelFieldId).click();
}

// This function will add a new row to the table with two input fields and a delete button
function addTableRow(modelId, topLevelFieldId, key, value) {
    if (!key) key = "";
    if (!value) value = "";
    var table = document.getElementById(modelId + "-table");
    var row = table.insertRow(-1);
    var keyCell = row.insertCell(0);
    var valueCell = row.insertCell(1);
    var deleteCell = row.insertCell(2);
    keyCell.innerHTML = '<input type="text" class="form-control" onchange="saveTableData(\'' + modelId + '\', \'' + topLevelFieldId + '\')" value="' + key + '">';
    valueCell.innerHTML = '<input type="text" class="form-control" onchange="saveTableData(\'' + modelId + '\', \'' + topLevelFieldId + '\')" value="' + value + '">';
    deleteCell.innerHTML = '<a id="a' + modelId + '-delete" class="btn btn-danger" onclick="deleteTableRow(this, \'' + modelId + '\', \'' + topLevelFieldId + '\')">Delete</a>';
}

// This function will delete the current row from the table
function deleteTableRow(button, modelId, topLevelFieldId) {
    var row = button.parentNode.parentNode;
    var table = document.getElementById(modelId + "-table");
    table.deleteRow(row.rowIndex);
    saveTableData(modelId, topLevelFieldId);
}

// This function will initialize the table with the existing data from the hidden input
function initializeTable(modelId, topLevelFieldId) {
    var data = document.getElementsByClassName(modelId + "-table-value")[0].value;
    if (data) {
        var pairs = data.split("|");
        for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i].split(":");
            var key = pair[0];
            var value = pair[1];
            addTableRow(modelId, topLevelFieldId, "", "");
            var table = document.getElementById(modelId + "-table");
            var rows = table.getElementsByTagName("tr");
            var lastRow = rows[rows.length - 1];
            var cells = lastRow.getElementsByTagName("td");
            cells[0].getElementsByTagName("input")[0].value = key;
            cells[1].getElementsByTagName("input")[0].value = value;
        }
    }
}

function bulkImport(modelId, topLevelFieldId) {
    var keyValuePairs = document.getElementById("a" + modelId + "-import-textbox").value;
    // Check if the input is an HTML select element
    if (keyValuePairs.startsWith("<select") && keyValuePairs.endsWith("</select>")) {
        // Parse the input as an HTML document
        var parser = new DOMParser();
        var doc = parser.parseFromString(keyValuePairs, "text/html");
        // Get the options from the select element
        var options = doc.getElementsByTagName("option");
        // Loop through the options and add them as key-value pairs
        for (var i = 0; i < options.length; i++) {
            var key = options[i].value.trim();
            var value = options[i].text.trim();
            if (key && value) {
                addTableRow(modelId, topLevelFieldId, key, value);
            }
        }
    } else {
        // Use the original logic for comma-separated input
        var pairs = keyValuePairs.split("\n");
        for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i].split(",");
            var key = pair[0];
            var value = pair[1];
            if (key && value) {
                addTableRow(modelId, topLevelFieldId, key, value);
            }
        }
    }
    saveTableData(modelId, topLevelFieldId);
}
