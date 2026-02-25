import { Link } from "react-router-dom";
import {
  Users,
  Frown,
  Zap,
  Rocket,
  Wrench,
  CloudCheck,
  Info,
  Github,
  ChevronDown
} from "lucide-react";
import "./Home.css";

function Home() {
  return (
    <div className="home-page">
      <div className="hero-section">
        <img
          src="/potato_mascot_500.png"
          alt="Hot Foetato Mascot"
          className="potato-animation"
        />
        <h1 className="title">HOT FOETATO</h1>

        <div className="features">
          <div className="feature">
            <Users size={24} />
            <h3>2-4 Players</h3>
            <p>Multiplayer mayhem with friends</p>
          </div>

          <div className="feature">
            <Zap size={24} />
            <h3>Fast-Paced</h3>
            <p>Quick rounds of EXPLOSIVE action</p>
          </div>

          <div className="feature">
            <Frown size={24} />
            <h3>Score Tracking</h3>
            <p>See who's the ultimate loser!</p>
          </div>
        </div>

        <div className="how-to-play">
          <h2>How to Play</h2>
          <div className="steps">
            <div className="step">
              <div className="step-number">1</div>
              <h3>Create a Room</h3>
              <p>Join or create a game room</p>
            </div>

            <div className="step">
              <div className="step-number">2</div>
              <h3>Pass the Potato</h3>
              <p>Click other players to pass</p>
            </div>

            <div className="step">
              <div className="step-number">3</div>
              <h3>Don't Explode!</h3>
              <p>Avoid being the last one holding it</p>
            </div>
          </div>
        </div>

        <div className="button-container">
          <Link to="/play" className="play-button">
            Play Game
          </Link>
        </div>

        <div className="scroll-indicator">
          <div className="scroll-indicator">
            <ChevronDown size={32} />
          </div>
        </div>
      </div>

      <div className="portfolio-section">
        <h2>About This Project</h2>

        {/* ✅ KEY FEATURES LIST */}
        <div className="key-features-box">
          <h3>
            <Info size={24} /> Key Features
          </h3>
          <ul className="features-list">
            <li>• Real-time WebSocket synchronization</li>
            <li>• Room-based matchmaking system</li>
            <li>• Live scoreboard tracking</li>
            <li>• React-hosted Unity WebGL deployment</li>
          </ul>
        </div>

        <div className="feature-highlights">
          <div className="highlight-card">
            <h3>
              <CloudCheck size={24} /> Server-Authoritative Architecture
            </h3>
            <p>
              All game state is validated on the backend to prevent desync and
              cheating. The server manages rooms, timers, player elimination,
              and score updates, broadcasting real-time state to connected
              clients.
            </p>
          </div>

          <div className="highlight-card">
            <h3>
              <Rocket size={24} /> Scalable Multiplayer Backend
            </h3>
            <p>
              Designed with room-based session management and stateless client
              updates, allowing horizontal scaling of game instances in
              production.
            </p>
          </div>

          <div className="highlight-card">
            <h3>
              <Wrench size={24} /> Technical Challenges
            </h3>
            <p>
              Solved WebGL networking limitations, synchronized timers across
              clients, handled player disconnects gracefully, and ensured
              consistent state updates between Unity and the WebSocket server.
            </p>
          </div>
        </div>

        {/* ✅ TECH STACK */}
        <div className="tech-summary">
          <p>
            <strong>Built with:</strong> Unity (C#), React (TypeScript),
            Node.js, WebSockets, Vite, React Router
          </p>
        </div>

        <div className="github-button-container">
          <a
            href="https://github.com/Jarrettsmao/Hot-Foetato"
            target="_blank"
            rel="noopener noreferrer"
            className="github-button"
          >
            <Github size={24} /> View Source Code →
          </a>
        </div>
      </div>

      <footer className="footer">
        <p>Made with 🔥 and 🥔 | © 2026</p>
      </footer>
    </div>
  );
}

export default Home;
