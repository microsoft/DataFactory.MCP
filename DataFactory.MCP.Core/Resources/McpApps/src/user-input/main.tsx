import React from "react";
import ReactDOM from "react-dom/client";
import { UserInputApp } from "./UserInputApp";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <UserInputApp appName="User Input" />
  </React.StrictMode>,
);
