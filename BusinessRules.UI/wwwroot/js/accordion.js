// A function to find all the accordions on the page and return an array of their ids
function findAllAccordions() {
    let accordions = document.querySelectorAll(".accordion");
    let ids = [];
    for (let accordion of accordions) {
        ids.push(accordion.id);
    }
    return ids;
}

// A function to get the state of an accordion (open or closed) by its id
function getAccordionState(id) {
    let button = document.querySelector("#" + id + " .accordion-button");
    if (button && button.classList) {
        return !button.classList.contains("collapsed");
    }
}

// A function to set the state of an accordion (open or close) by its id and state
function setAccordionState(id, state) {
    let button = document.querySelector("#" + id + " .accordion-button");
    let collapse = document.querySelector("#" + id + " .accordion-collapse");
    if (state && button) {
        // Open the accordion
        button.classList.remove("collapsed");
        collapse.classList.add("show");
        collapse.setAttribute("aria-expanded", "true");
    } else if (button) {
        // Close the accordion
        button.classList.add("collapsed");
        collapse.classList.remove("show");
        collapse.setAttribute("aria-expanded", "false");
    }
}

function saveAccordionState() {
    let ids = findAllAccordions();
    let state = {};
    for (let id of ids) {
        state[id] = getAccordionState(id);
    }
    sessionStorage.setItem("accordionState", JSON.stringify(state));
}

function restoreAccordionState() {
    let state = JSON.parse(sessionStorage.getItem("accordionState"));
    if (state) {
        for (let id in state) {
            setAccordionState(id, state[id]);
        }
    }
}