//import './App.css'
import { useNavigate } from 'react-router'

function Home() {
    const navigate = useNavigate();

    return (
        <>
            <div>
                <h1> Welcome to my website! </h1>
                <p>The homepage is still underconstruction but checkout my photos below. </p>
                <button onClick={() => navigate("photos")} >Photos </button>
            </div>
        </>
    )
}

export default Home
