// Add this function for auto-suggest functionality for GroupName input
function groupNameAutoSuggest(event, index, rawGroupNames) {
    const input = event.target;
    const allGroupNames = JSON.parse(rawGroupNames);
    const groupNameList = document.getElementById('groupNameList' + index);
    groupNameList.innerHTML = '';

    var inputLower = "";
    if (input && input.value) {
        inputLower = input.value.toLowerCase();
    }

    // Filter the group names based on the user input
    const filteredGroupNames =
        allGroupNames.filter(groupName => groupName && groupName.toLowerCase().includes(inputLower));

    const fragment = document.createDocumentFragment();

    var currentInputIsAlreadyAGroup = false;

    filteredGroupNames.forEach(groupName => {
        if (input && input.value && groupName === input.value) {
            currentInputIsAlreadyAGroup = true;
        } else {
            const option = document.createElement('div');
            option.classList.add('group-name-option');
            option.innerHTML = "Existing Group: <strong>" + groupName + "</strong>";
            option.addEventListener('click', () => selectGroupName(input, index, groupName));
            fragment.appendChild(option);
        }
    });

    if (input && input.value && !currentInputIsAlreadyAGroup) {
        const option = document.createElement('div');
        option.classList.add('group-name-option');
        option.innerHTML = "Add New Group: <strong>" + input.value + "</strong>";
        option.addEventListener('click', () => selectGroupName(input, index, input.value));
        fragment.appendChild(option);
    } else if (!input || !input.value || input.value === "") {
        const option = document.createElement('div');
        option.classList.add('group-name-option');
        option.innerHTML = "Start typing to add a new group name";
        option.addEventListener('click', () => selectGroupName(input, index, ""));
        fragment.appendChild(option);
        fragment.child
    }

    groupNameList.appendChild(fragment);
    groupNameList.style.display = 'block';
}

function selectGroupName(input, index, selectedGroupName) {
    console.log(selectedGroupName);
    input.value = selectedGroupName;
    document.getElementById('groupNameList' + index).innerHTML = '';
    input.dispatchEvent(new Event('change'));
}
