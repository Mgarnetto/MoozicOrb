document.addEventListener('DOMContentLoaded', () => {

    // =====================================================================
    // GLOBAL ELEMENTS AND VARIABLES
    // =====================================================================
    const body = document.body;
    const toggler = document.querySelector('.navbar__toggler');
    const sidebar = document.getElementById('sidebar-navigation');
    const submenuToggles = document.querySelectorAll('.sidebar__nav .has-submenu > a');

    // Calendar elements
    const calendarContainer = document.querySelector('.calendar-container');
    const daysContainer = calendarContainer.querySelector('.days');
    const monthDisplay = calendarContainer.querySelector('.date-display');
    const prevButton = calendarContainer.querySelector('.nav-button.prev');
    const nextButton = calendarContainer.querySelector('.nav-button.next');
    const todayButton = calendarContainer.querySelector('.today-btn');
    const gotoDateInput = calendarContainer.querySelector('#date-input');
    const gotoButton = calendarContainer.querySelector('.goto-btn');
    const eventDayDisplay = calendarContainer.querySelector('.event-day');
    const eventDateDisplay = calendarContainer.querySelector('.event-date');
    const eventsListContainer = calendarContainer.querySelector('.events-list');
    const addEventForm = calendarContainer.querySelector('.add-event-form');
    const addEventTitleInput = calendarContainer.querySelector('#event-title');
    const addEventDescriptionInput = calendarContainer.querySelector('#event-description');
    const addEventLocationInput = calendarContainer.querySelector('#event-location');
    const addEventDateInput = calendarContainer.querySelector('#add-event-date');
    const addEventTimeFromInput = calendarContainer.querySelector('#event-time-from');
    const addEventTimeToInput = calendarContainer.querySelector('#event-time-to');
    const addEventContactInput = calendarContainer.querySelector('#event-contact');

    // Calendar state variables
    let currentDate = new Date();
    let selectedDate = new Date();
    let events = []; // Array to store event objects
    let selectedDayElement = null;


    // =====================================================================
    // SIDEBAR AND COLLAPSIBLE NAVIGATION LOGIC
    // =====================================================================

    // Function to toggle the main sidebar
    toggler.addEventListener('click', () => {
        const isExpanded = toggler.getAttribute('aria-expanded') === 'true';
        toggler.setAttribute('aria-expanded', !isExpanded);
        sidebar.classList.toggle('is-open');
        body.classList.toggle('sidebar-open');
    });

    // Functionality for multi-level submenus
    submenuToggles.forEach(toggle => {
        toggle.addEventListener('click', (e) => {
            e.preventDefault();

            const parentLi = toggle.closest('.has-submenu');
            const submenu = parentLi.querySelector('.submenu');
            const isExpanded = toggle.getAttribute('aria-expanded') === 'true';

            // Close other open submenus at the same level for cleaner UX
            submenuToggles.forEach(otherToggle => {
                const otherParentLi = otherToggle.closest('.has-submenu');
                if (otherParentLi !== parentLi && otherParentLi.classList.contains('open')) {
                    otherParentLi.classList.remove('open');
                    otherToggle.setAttribute('aria-expanded', 'false');
                    otherParentLi.querySelector('.submenu').style.maxHeight = '0';
                }
            });

            // Toggle the current submenu
            parentLi.classList.toggle('open');
            toggle.setAttribute('aria-expanded', !isExpanded);

            if (isExpanded) {
                // If currently expanded, collapse it
                submenu.style.maxHeight = '0';
            } else {
                // If collapsed, expand it to its full scroll height
                submenu.style.maxHeight = submenu.scrollHeight + 'px';
            }
        });
    });

    // General collapsible functionality
    document.querySelectorAll('.collapsible').forEach(collapsible => {
        collapsible.addEventListener('click', () => {
            collapsible.classList.toggle('is-active');
            const content = collapsible.nextElementSibling;

            if (content.style.maxHeight) {
                content.style.maxHeight = null;
                collapsible.setAttribute('aria-expanded', 'false');
            } else {
                content.style.maxHeight = content.scrollHeight + 'px';
                collapsible.setAttribute('aria-expanded', 'true');
            }
        });
    });


    // =====================================================================
    // CALENDAR AND EVENT MANAGEMENT LOGIC
    // =====================================================================

    /**
     * Renders the days for the current month in the calendar grid.
     */
    function renderCalendar() {
        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();

        // Update the month and year display
        monthDisplay.textContent = currentDate.toLocaleString('default', { month: 'long', year: 'numeric' });

        // Clear previous days
        daysContainer.innerHTML = '';

        const firstDayOfMonth = new Date(year, month, 1).getDay(); // 0 = Sunday, 6 = Saturday
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const daysInPrevMonth = new Date(year, month, 0).getDate();
        const today = new Date();

        // Render previous month's days
        for (let i = firstDayOfMonth; i > 0; i--) {
            const day = document.createElement('div');
            day.classList.add('day', 'day--prev-month');
            day.textContent = daysInPrevMonth - i + 1;
            daysContainer.appendChild(day);
        }

        // Render current month's days
        for (let i = 1; i <= daysInMonth; i++) {
            const day = document.createElement('div');
            day.classList.add('day');
            day.setAttribute('role', 'gridcell');
            day.textContent = i;
            day.dataset.date = `${year}-${month + 1}-${i}`;

            // Check for today's date
            if (year === today.getFullYear() && month === today.getMonth() && i === today.getDate()) {
                day.classList.add('is-today');
            }

            // Check if this day has an event
            if (events.some(event => {
                const eventDate = new Date(event.date);
                return eventDate.getFullYear() === year && eventDate.getMonth() === month && eventDate.getDate() === i;
            })) {
                day.classList.add('has-event');
            }

            daysContainer.appendChild(day);
        }

        // Render next month's days to fill the grid
        const totalDays = daysContainer.children.length;
        const daysToFill = 42 - totalDays; // 6 weeks * 7 days
        for (let i = 1; i <= daysToFill; i++) {
            const day = document.createElement('div');
            day.classList.add('day', 'day--next-month');
            day.textContent = i;
            daysContainer.appendChild(day);
        }

        // Select the day that was previously selected (if any)
        const activeDayElement = daysContainer.querySelector(`[data-date="${selectedDate.getFullYear()}-${selectedDate.getMonth() + 1}-${selectedDate.getDate()}"]`);
        if (activeDayElement) {
            activeDayElement.classList.add('is-active');
            selectedDayElement = activeDayElement;
        }

        // Re-render events for the currently selected date
        renderEventsForSelectedDay();
    }

    /**
     * Renders the events for the currently selected date in the events list.
     */
    function renderEventsForSelectedDay() {
        // Update the current day display
        eventDayDisplay.textContent = selectedDate.toLocaleString('default', { weekday: 'short' });
        eventDateDisplay.textContent = selectedDate.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' });

        // Clear previous events
        eventsListContainer.innerHTML = '';

        const selectedDateString = `${selectedDate.getFullYear()}-${selectedDate.getMonth() + 1}-${selectedDate.getDate()}`;

        const eventsForDay = events.filter(event => {
            const eventDate = new Date(event.date);
            const eventDateString = `${eventDate.getFullYear()}-${eventDate.getMonth() + 1}-${eventDate.getDate()}`;
            return eventDateString === selectedDateString;
        });

        if (eventsForDay.length > 0) {
            eventsForDay.forEach(event => {
                const eventHtml = `
                    <div class="event-item">
                        <div class="event-title">${event.title}</div>
                        ${event.description ? `<div class="event-description">${event.description}</div>` : ''}
                        ${event.location ? `<div class="event-location">Location: ${event.location}</div>` : ''}
                        ${event.timeFrom || event.timeTo ? `<div class="event-time">Time: ${event.timeFrom} - ${event.timeTo}</div>` : ''}
                        ${event.contact ? `<div class="event-contact">Contact: ${event.contact}</div>` : ''}
                    </div>
                `;
                eventsListContainer.innerHTML += eventHtml;
            });
        } else {
            eventsListContainer.innerHTML = `<div class="no-events">No events for this day.</div>`;
        }
    }

    /**
     * Handles the logic for adding a new event.
     * @param {Event} e The form submission event.
     */
    function handleAddEvent(e) {
        e.preventDefault();

        const title = addEventTitleInput.value.trim();
        if (!title) {
            console.error('Event title is required.');
            return;
        }

        // Use the currently selected date if the input is empty
        const eventDateValue = addEventDateInput.value;
        let eventDate = selectedDate;

        if (eventDateValue) {
            const [month, day, year] = eventDateValue.split('/').map(Number);
            if (!isNaN(month) && !isNaN(day) && !isNaN(year)) {
                eventDate = new Date(year, month - 1, day);
            } else {
                console.error('Invalid date format. Please use mm/dd/yyyy.');
                return;
            }
        }

        const newEvent = {
            title: title,
            description: addEventDescriptionInput.value.trim(),
            location: addEventLocationInput.value.trim(),
            date: eventDate,
            timeFrom: addEventTimeFromInput.value.trim(),
            timeTo: addEventTimeToInput.value.trim(),
            contact: addEventContactInput.value.trim()
        };

        events.push(newEvent);

        // Re-render the calendar and events list
        renderCalendar();
        renderEventsForSelectedDay();

        // Clear the form
        addEventForm.reset();
    }


    // =====================================================================
    // CALENDAR EVENT LISTENERS
    // =====================================================================

    // Previous and Next month navigation
    prevButton.addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() - 1);
        renderCalendar();
    });

    nextButton.addEventListener('click', () => {
        currentDate.setMonth(currentDate.getMonth() + 1);
        renderCalendar();
    });

    // Go to a specific date
    gotoButton.addEventListener('click', () => {
        const dateInput = gotoDateInput.value.split('/');
        const month = parseInt(dateInput[0], 10) - 1;
        const year = parseInt(dateInput[1], 10);

        if (!isNaN(month) && !isNaN(year)) {
            currentDate.setMonth(month);
            currentDate.setFullYear(year);
            renderCalendar();
        } else {
            console.error('Invalid date format. Please use mm/yyyy.');
        }
    });

    // Go to today's date
    todayButton.addEventListener('click', () => {
        currentDate = new Date();
        selectedDate = new Date();
        renderCalendar();
    });

    // Select a day
    daysContainer.addEventListener('click', (e) => {
        const dayElement = e.target.closest('.day');
        if (dayElement && !dayElement.classList.contains('day--prev-month') && !dayElement.classList.contains('day--next-month')) {
            // Remove 'is-active' from the previous selection
            if (selectedDayElement) {
                selectedDayElement.classList.remove('is-active');
            }

            // Add 'is-active' to the new selection
            dayElement.classList.add('is-active');
            selectedDayElement = dayElement;

            // Update selectedDate and re-render events
            const dateParts = dayElement.dataset.date.split('-').map(Number);
            selectedDate = new Date(dateParts[0], dateParts[1] - 1, dateParts[2]);
            addEventDateInput.value = `${dateParts[1]}/${dateParts[2]}/${dateParts[0]}`; // Set the date input field
            renderEventsForSelectedDay();
        }
    });

    // Handle add event form submission
    addEventForm.addEventListener('submit', handleAddEvent);

    // Initial render of the calendar
    renderCalendar();

    // =====================================================================
    // AMCHARTS WORLD MAP LOGIC (UNCHANGED)
    // =====================================================================
    am5.ready(function () {
        const root = am5.Root.new("chartdiv");
        root.setThemes([am5themes_Animated.new(root)]);

        const chart = root.container.children.push(am5map.MapChart.new(root, {
            panX: "rotateX",
            panY: "rotateY",
            projection: am5map.geoOrthographic(),
            paddingBottom: 20,
            paddingTop: 20,
            paddingLeft: 20,
            paddingRight: 20
        }));

        const polygonSeries = chart.series.push(am5map.MapPolygonSeries.new(root, {
            geoJSON: am5geodata_worldLow
        }));

        polygonSeries.mapPolygons.template.setAll({
            tooltipText: "{name}",
            toggleKey: "active",
            interactive: true,
            fill: am5.color(0x6794dc),
            stroke: am5.color(0xffffff),
            strokeWidth: 0.5
        });

        polygonSeries.mapPolygons.template.states.create("hover", {
            fill: root.interfaceColors.get("primaryButtonHover")
        });

        polygonSeries.mapPolygons.template.states.create("active", {
            fill: root.interfaceColors.get("primaryButtonHover")
        });

        const backgroundSeries = chart.series.push(am5map.MapPolygonSeries.new(root, {}));
        backgroundSeries.mapPolygons.template.setAll({
            fill: root.interfaceColors.get("alternativeBackground"),
            fillOpacity: 0.1,
            strokeOpacity: 0
        });
        backgroundSeries.data.push({
            geometry: am5map.getGeoRectangle(90, 180, -90, -180)
        });

        const graticuleSeries = chart.series.unshift(am5map.GraticuleSeries.new(root, {
            step: 10
        }));
        graticuleSeries.mapLines.template.set("strokeOpacity", 0.1);

        let previousPolygon;
        polygonSeries.mapPolygons.template.on("active", function (active, target) {
            if (previousPolygon && previousPolygon !== target) {
                previousPolygon.set("active", false);
            }
            if (target.get("active")) {
                selectCountry(target.dataItem.get("id"));
            }
            previousPolygon = target;
        });

        function selectCountry(id) {
            const dataItem = polygonSeries.getDataItemById(id);
            const target = dataItem ? dataItem.get("mapPolygon") : null;
            if (target) {
                const centroid = target.geoCentroid();
                if (centroid) {
                    chart.animate({
                        key: "rotationX",
                        to: -centroid.longitude,
                        duration: 1500,
                        easing: am5.ease.inOut(am5.ease.cubic)
                    });
                    chart.animate({
                        key: "rotationY",
                        to: -centroid.latitude,
                        duration: 1500,
                        easing: am5.ease.inOut(am5.ease.cubic)
                    });
                }
            }
        }
        chart.appear(1000, 100);
    }); // end am5.ready()
});

