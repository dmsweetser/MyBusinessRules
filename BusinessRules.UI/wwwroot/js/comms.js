function ApplyCreditCode(event) {
    event.preventDefault();
    event.stopPropagation();

    var buttonToClick = event.target;

    if (buttonToClick
        && buttonToClick.getAttribute("formAction")) {

        var formAction = buttonToClick.getAttribute("formAction");
        var formToSubmit = new FormData();
        formToSubmit.append("codeId", document.getElementById("creditCode").value);

        fetch(formAction, {
            method: "POST",
            body: formToSubmit
        })
            .then((response) => {
                return;
            });
    }
}

function SubmitAndRefreshPartial(event, targetElementId, formElementId, otherElementsToClick) {

    event.preventDefault();
    event.stopPropagation();

    var buttonToClick = event.target;

    if (buttonToClick
        && buttonToClick.getAttribute("formAction")) {

        var parentTab = buttonToClick.closest('.tab-pane');
        var currentScrollPosition = 0;
        if (parentTab && parentTab.scrollTop) {
            currentScrollPosition = parentTab.scrollTop;
        }
        var formAction = buttonToClick.getAttribute("formAction");

        var formData;
        if (formElementId !== "") {
            formData = new FormData(document.getElementById(formElementId));
        } else {
            formData = new FormData();
        }

        fetch(formAction, {
            method: "POST",
            body: formData
        })
            .then((response) => {
                if (response.redirected) {
                    window.location.href = response.url;
                    return;
                }
                else if (!response.ok) {
                    throw Error(response.statusText)
                } else {
                    return response.text();
                }
            })
            .then((data) => {
                if (targetElementId !== null) {
                    if (data) {
                        var targetElement = document.getElementById(targetElementId);
                        targetElement.innerHTML = data;

                        var scripts = targetElement.getElementsByTagName("script");
                        for (var i = 0; i < scripts.length; ++i) {
                            var script = scripts[i];
                            window.eval(script.innerHTML);
                        }
                    }

                    UpdateFieldSelector();
                    restoreAccordionState();

                    var lastFocusedElementId = sessionStorage.getItem('lastFocusedElementId');
                    if (lastFocusedElementId && document.querySelector('#' + lastFocusedElementId)) {
                        var lastFocusedElement = document.querySelector('#' + lastFocusedElementId);
                        var attemptFocusOn = lastFocusedElement.getAttribute("data-attempt-focus-on");
                        if (attemptFocusOn
                            && document.getElementsByClassName(attemptFocusOn)[0]
                            && document.getElementsByClassName(attemptFocusOn)[0].value === '') {
                            document.getElementsByClassName(attemptFocusOn)[0].focus();
                            document.getElementsByClassName(attemptFocusOn)[0]
                                .scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
                        } else {
                            lastFocusedElement.focus();
                            lastFocusedElement
                                .scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
                        }
                    } else {
                        if (parentTab && parentTab.scrollTop) {
                            parentTab.scrollTop = currentScrollPosition;
                        }
                    }

                    if (otherElementsToClick) {
                        [].forEach.call(otherElementsToClick, function (elementId) {
                            document.getElementById(elementId).click();
                        });
                    }
                }                
            }).catch(error => {
                console.log(error);
            });
    }
}

function RefreshPartial(event, targetElementId) {

    event.preventDefault();
    event.stopPropagation();

    var buttonToClick = event.target;

    if (buttonToClick
        && buttonToClick.getAttribute("formAction")) {

        var parentTab = buttonToClick.closest('.tab-pane');
        var currentScrollPosition = parentTab.scrollTop;
        var formAction = buttonToClick.getAttribute("formAction");

        fetch(formAction, {
            method: "POST"
        })
            .then((response) => {
                if (response.redirected) {
                    window.location.href = response.url;
                    return;
                }
                else if (!response.ok) {
                    throw Error(response.statusText)
                } else {
                    return response.text();
                }
            })
            .then((data) => {
                if (data) {
                    var targetElement = document.getElementById(targetElementId);
                    targetElement.innerHTML = data;

                    var scripts = targetElement.getElementsByTagName("script");
                    for (var i = 0; i < scripts.length; ++i) {
                        var script = scripts[i];
                        window.eval(script.innerHTML);
                    }
                }

                UpdateFieldSelector();
                restoreAccordionState();

                setTimeout(() => {
                    var lastFocusedElementId = sessionStorage.getItem('lastFocusedElementId');

                    if (lastFocusedElementId && document.querySelector('#' + lastFocusedElementId)) {
                        var lastFocusedElement = document.querySelector('#' + lastFocusedElementId);
                        var attemptFocusOn = lastFocusedElement.getAttribute("data-attempt-focus-on");
                        if (attemptFocusOn
                            && document.getElementsByClassName(attemptFocusOn)[0]
                            && document.getElementsByClassName(attemptFocusOn)[0].value === '') {
                            document.getElementsByClassName(attemptFocusOn)[0].focus();
                            document.getElementsByClassName(attemptFocusOn)[0]
                                .scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
                        } else {
                            lastFocusedElement.focus();
                            lastFocusedElement
                                .scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'nearest' });
                        }
                    } else {
                        parentTab.scrollTop = currentScrollPosition;
                    }
                }, 250);

            }).catch(error => {
                console.log(error);
            });
    }
}

function uploadSerializedObject(event, id) {

    event.preventDefault();
    event.stopPropagation();

    // Get the value of the textarea element
    var serializedObject = document.getElementById(id).value;

    var stringifiedObject = validateContent(serializedObject);

    if (stringifiedObject === "") {
        showAlertModal("What you provided is not valid JSON - please try again");
        return;
    }

    var formToSubmit = new FormData();
    formToSubmit.append("stringifiedObject", stringifiedObject);

    // Create an options object with the method, headers and body
    var options = {
        method: "POST",
        body: formToSubmit
    };

    // Use the fetch API to send the request to the controller action
    fetch("/Field/BuildTopLevelField", options)
        // Reload the page
        .then(response => {
            window.location.reload();
            return;
        })
        // Catch any error and display an error message
        .catch(error => {
            console.log(error);
        });
}