function filterComponents(event, ruleIndex, componentSelectId) {
    const input = event.target;
    const selectElement = document.getElementById(componentSelectId);
    const components = Array.from(selectElement.options).map(option => ({
        Key: option.value,
        Text: option.innerText
    }));
    const componentList = document.getElementById('componentList' + ruleIndex);
    const inputLower = input.value.toLowerCase();

    componentList.innerHTML = '';

    const filteredComponents = components.filter(component => component.Text.toLowerCase().includes(inputLower));
    if (filteredComponents.length === 0) {
        return;
    }

    const fragment = document.createDocumentFragment();

    filteredComponents.forEach(component => {
        const option = document.createElement('div');
        option.classList.add('component-option');
        option.textContent = component.Text;
        option.value = component.Key;
        option.addEventListener('click', () => selectComponent(ruleIndex, component.Key));
        fragment.appendChild(option);
    });

    componentList.appendChild(fragment);

    if (event.key && (event.key === 'Tab' || event.key === 'Enter') && filteredComponents.length === 1) {
        event.preventDefault();
        event.stopPropagation();
        selectComponent(ruleIndex, filteredComponents[0].Key);
    } else if (event.key === 'Backspace' && input.value === '') {
        document.getElementById('removeLatestComponent' + ruleIndex).click();
    }
}
function decodeHtmlEntities(str) {
    const element = document.createElement('div');
    element.innerHTML = str;
    return element.textContent;
}

function selectComponent(ruleIndex, selectedComponent) {
    const input = document.getElementById('componentInput' + ruleIndex);
    input.value = selectedComponent;

    const addNewComponentBtn = document.getElementById('addNewComponent' + ruleIndex);
    const currentFormAction = `${addNewComponentBtn.getAttribute('formaction')}&nextcomponentkey=${selectedComponent}`;
    addNewComponentBtn.setAttribute('formaction', currentFormAction);
    addNewComponentBtn.click();

    const componentList = document.getElementById('componentList' + ruleIndex);
    componentList.innerHTML = '';
    input.value = '';
}