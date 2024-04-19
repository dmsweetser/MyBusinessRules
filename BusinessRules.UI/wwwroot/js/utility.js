function showAlertModal(message) {
    $('#alertModalBody').text(message);
    $('#alertModal').modal('show');
}

function CopyValueToClipboard(event, copyText) {
    navigator.clipboard.writeText(copyText);
    event.preventDefault();
    event.stopPropagation();
}

function CheckValid(event, validationId) {
    if (!event.target.checkValidity()
        && document.getElementById(validationId)) {
        event.preventDefault();
        document.getElementById(validationId).classList.remove("d-none");
        return false;
    } else if (document.getElementById(validationId)) {
        document.getElementById(validationId).classList.add("d-none");
        return true;
    }
}

function ConfirmBeforeExecuting(event, message, callback) {

    event.preventDefault();
    event.stopPropagation();

    var displayText = "Are you sure?";
    if (message !== "") displayText = message;

    showConfirmationModal(displayText, function (confirmed) {
        if (confirmed) {
            if (callback) callback();
        } else {
            //Nothing happens
        }
    });
}

function showConfirmationModal(message, onConfirm) {
    $('#confirmationModalBody').text(message);

    $('#confirmButton').off('click').on('click', function () {
        onConfirm(true);  // Pass 'true' to indicate confirmation
        $('#confirmationModal').modal('hide');
    });

    $('#cancelButton').off('click').on('click', function () {
        onConfirm(false);  // Pass 'false' to indicate cancellation
        $('#confirmationModal').modal('hide');
    });

    $('#confirmationModal').modal('show');
}

// A function that takes a string and returns a stringified version of the successfully parsed data, 
// or else blank if not valid
function validateContent(content) {
    // Try to parse the content as JSON
    try {
        var jsonData = JSON.parse(content);
        // If no exception is thrown, return the stringified JSON data
        return JSON.stringify(jsonData, null, 2);
    }
    // Catch any exception that occurs
    catch (e) {
        // Return blank
        return "";
    }
}