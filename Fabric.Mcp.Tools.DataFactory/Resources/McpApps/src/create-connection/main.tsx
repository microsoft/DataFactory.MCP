import React from "react";
import ReactDOM from "react-dom/client";
import { CreateConnectionApp } from "./CreateConnectionApp";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <CreateConnectionApp appName="Create Connection" />
  </React.StrictMode>,
);
