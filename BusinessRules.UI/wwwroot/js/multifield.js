function SetVisibleApp(element) {
    var selectedApp = element.value;

    if (selectedApp === "AddNewTopLevelField") {
        document.getElementById("addNewTopLevelField").click();
    } else {
        sessionStorage.setItem('selectedTopLevelField', selectedApp);
        var fieldListElements = document.getElementsByClassName("fieldList");
        [].forEach.call(fieldListElements, element => {
            element.value = selectedApp;
        });

        var ruleContainers = document.getElementsByClassName("ruleContainer");
        [].forEach.call(ruleContainers, element => {
            if (element.id === "ruleContainer" + selectedApp) {
                element.classList.remove("d-none");
            } else {
                element.classList.add("d-none");
            }
        });

        var fieldContainers = document.getElementsByClassName("fieldContainer");
        [].forEach.call(fieldContainers, element => {
            if (element.id === "fieldContainer" + selectedApp) {
                element.classList.remove("d-none");
            } else {
                element.classList.add("d-none");
            }
        });

        var endUserContainers = document.getElementsByClassName("endUserContainer");
        [].forEach.call(endUserContainers, element => {
            if (element.id === "endUserContainer" + selectedApp) {
                element.classList.remove("d-none");
            } else {
                element.classList.add("d-none");
            }
        });
    }
}

function UpdateFieldSelector() {
    var selectedTopLevelField = sessionStorage.getItem('selectedTopLevelField');
    var fieldSelector = document.getElementById("manageFieldSelector");
    if (selectedTopLevelField
        && selectedTopLevelField != ""
        && fieldSelector
        && fieldSelector.querySelector('option[value="' + selectedTopLevelField + '"]')
    ) {
        //Restores the current selection
        fieldSelector.value = selectedTopLevelField;
        var ruleSelector = document.getElementById("manageRuleSelector");
        ruleSelector.value = selectedTopLevelField;
        var liveDemoSelector = document.getElementById("liveDemoSelector");
        liveDemoSelector.value = selectedTopLevelField;
        SetVisibleApp(fieldSelector);
    } else {
        //Defaults to the first option available
        var fieldSelector = document.getElementById("manageFieldSelector");
        if (fieldSelector) {
            var firstOption = fieldSelector.querySelector('option:not([value=""])');
            if (firstOption) {
                firstOption.selected = true;
                var ruleSelector = document.getElementById("manageRuleSelector");
                var ruleOption = fieldSelector.querySelector('option:not([value=""])');
                ruleSelector.value = ruleOption.value;
                var liveDemoSelector = document.getElementById("liveDemoSelector");
                var liveDemoOption = fieldSelector.querySelector('option:not([value=""])');
                liveDemoSelector.value = liveDemoOption.value;
                SetVisibleApp(fieldSelector);
            }
        }
    }
}