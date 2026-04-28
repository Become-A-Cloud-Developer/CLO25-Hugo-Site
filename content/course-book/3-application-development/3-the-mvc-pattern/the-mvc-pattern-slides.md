+++
title = "The MVC Pattern"
program = "CLO"
cohort = "25"
courses = ["BCD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## The MVC Pattern
Part III — Application Development

---

## Why MVC exists
- Input handling, business state, and rendering change for **different reasons**
- Mixing them yields code that is hard to test or modify
- MVC assigns each concern to a **distinct role**
- Roles communicate through narrow interfaces, not shared state

---

## The three roles
- **Controller** — receives input, coordinates work, returns a result
- **View** — renders state to HTML using a Razor template
- **Model** — the typed data passed from controller to view
- Rich domain models live in the service layer, not here

---

## Controllers
- A class ending in `Controller`, living in `Controllers/`
- Public methods are **actions** that handle requests
- Each action returns an `IActionResult`
- Attributes like `[HttpGet]`, `[HttpPost]`, `[Authorize]` constrain matching

---

## Routing
- Matches an incoming **URI to a controller action**
- Conventional template: `{controller=Home}/{action=Index}/{id?}`
- Attribute routing: `[Route]` and `[HttpGet("{id:int}")]` on the action
- Route values are extracted from the path before the action runs

---

## Model binding
- Converts request data into **typed action parameters**
- Sources: route values, query string, form fields, JSON body
- Validation attributes (`[Required]`, `[StringLength]`) populate `ModelState`
- Action body checks `ModelState.IsValid` before proceeding

---

## Action results
- `ViewResult` — render a Razor view
- `RedirectToActionResult` — 302 redirect after a successful POST
- `JsonResult` — serialize an object for an API response
- `NotFoundResult`, `BadRequestResult` — explicit error responses

---

## Razor view syntax
- `.cshtml` files mix HTML and C# expressions prefixed with `@`
- `@model Product` declares the strongly-typed model
- `@if`, `@foreach` for control flow inside templates
- Tag helpers (`asp-for`, `asp-action`) bind inputs to model properties

---

## The request flow
- Request → middleware → **routing** → controller → action → result → response
- Dependency injection supplies the controller's constructor parameters
- Each stage can be tested or replaced independently
- The framework handles serialization, status codes, and headers

---

## Questions?
