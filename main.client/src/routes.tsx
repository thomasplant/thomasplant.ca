import { createBrowserRouter } from "react-router";
import Home from "./pages/home";
import PhotosDashboard from "./pages/photosDashboard";

const router = createBrowserRouter([
    {
        path: "/",
        element: <Home />
    },
    {
        path: "/photos",
        element: <PhotosDashboard />
    },
]);

export default router;

