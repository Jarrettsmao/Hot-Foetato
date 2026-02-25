// import Component from "@/component.jsx"
import {BrowserRouter, Route, Routes} from "react-router-dom";
import Home from "./pages/Home.tsx";
import Play from "./pages/Play.tsx";
import NotFound from "./pages/NotFound.tsx";

function App() {
  return (
    <>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Home />}/>
          <Route path="/play" element={<Play />}/>
          {/* <Route path="*" element={<NotFound />}/> */}
        </Routes>
      </BrowserRouter>
    </>
  )
}

export default App;
