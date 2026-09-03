import { ArrowRight, BookOpen, Bot, Boxes, Check, Code2, GitFork, Heart, Layers3, PackageOpen, Rocket, Sparkles, Terminal } from "lucide-react";

const repositoryUrl = "https://github.com/JcscJosecarlossilvacoelho/dotnet-start";

const quickStarts = [
  { number: "01", icon: Terminal, title: "Install .NET", copy: "One SDK. Everything you need to build, run, and ship.", code: "winget install Microsoft.DotNet.SDK.10" },
  { number: "02", icon: Code2, title: "Create your app", copy: "Start with a clean, modern API — no ceremony required.", code: "dotnet new webapi -n MyApp" },
  { number: "03", icon: Rocket, title: "Run it", copy: "Your first .NET app is ready. Really — that is it.", code: "cd MyApp && dotnet run" },
];

const stack = [
  ["Web API", "ASP.NET Core", "Fast, cross-platform APIs"],
  ["Data", "Entity Framework Core", "Typed, productive data access"],
  ["Frontend", "Blazor", "Interactive web apps with C#"],
  ["Cloud", ".NET Aspire", "Observable, cloud-ready apps"],
];

export default function Home() {
  return (
    <main id="main-content">
      <div className="ambient ambient-one" /><div className="ambient ambient-two" />
      <nav className="nav shell" aria-label="Primary navigation">
        <a className="brand" href="#top" aria-label=".NET Start home"><span className="brand-mark">.n</span><span>dotnet<span className="brand-accent">start</span></span></a>
        <div className="nav-links"><a href="#start">Get started</a><a href="#stack">The stack</a><a href="#community">Community</a></div>
        <a className="nav-cta" href={repositoryUrl} target="_blank" rel="noreferrer"><GitFork size={16} /> Contribute</a>
      </nav>

      <section className="hero shell" id="top">
        <div className="hero-message">
          <div className="eyebrow"><Sparkles size={14} /> The community field guide to modern .NET</div>
          <h1>Build with <span>.NET</span>.<br />Skip the guesswork.</h1>
          <p className="hero-copy">Practical docs, one recommended stack, and trusted context for coding agents — a clear path from your first project to production.</p>
          <div className="hero-actions"><a className="button primary" href="#start">Follow the path <ArrowRight size={17} /></a><a className="button secondary" href={repositoryUrl} target="_blank" rel="noreferrer"><GitFork size={16} /> View on GitHub</a></div>
          <div className="hero-trust"><span><Check size={14} /> Open source</span><span><Check size={14} /> Community reviewed</span><span><Check size={14} /> Agent ready</span></div>
        </div>

        <div className="field-guide" aria-label="What .NET Start gives you">
          <div className="guide-bar"><span>YOUR PATH TO PRODUCTION</span><span className="guide-status"><i /> Maintained in public</span></div>
          <div className="guide-steps">
            <a href="#start"><span className="guide-number">01</span><span className="guide-icon"><BookOpen size={19} /></span><span><small>LEARN</small><strong>Practical guides</strong><em>Runnable steps, not theory</em></span><ArrowRight size={17} /></a>
            <a href="#stack"><span className="guide-number">02</span><span className="guide-icon"><Layers3 size={19} /></span><span><small>CHOOSE</small><strong>A recommended stack</strong><em>Good defaults, clearly explained</em></span><ArrowRight size={17} /></a>
            <a href="#community"><span className="guide-number">03</span><span className="guide-icon"><Bot size={19} /></span><span><small>BUILD</small><strong>Context for your agent</strong><em>Skills for Claude, Codex &amp; more</em></span><ArrowRight size={17} /></a>
          </div>
          <div className="guide-command"><span>❯</span><code>npx skills add dotnet-start</code><span className="guide-ready"><Check size={13} /> ready</span></div>
        </div>
      </section>

      <section className="manifesto" id="why"><div className="shell manifesto-inner">
        <div><p className="kicker">The idea</p><h2>Powerful doesn&apos;t<br />have to mean <em>complicated.</em></h2></div>
        <div className="manifesto-copy"><p>Microsoft gives you every option. We give you a place to begin.</p><p>No endless decision trees. No ten ways to build the same thing. Just a modern, community-tested path that gets you shipping.</p></div>
      </div></section>

      <section className="section shell" id="start">
        <div className="section-heading"><div><p className="kicker">Zero to running</p><h2>Your first app.<br />Three commands.</h2></div><p>Everything essential, nothing in the way. Explore the ecosystem after you have built something real.</p></div>
        <div className="steps">{quickStarts.map(({ number, icon: Icon, title, copy, code }) => <article className="step-card" key={number}><div className="step-top"><span>{number}</span><Icon size={20} /></div><h3>{title}</h3><p>{copy}</p><code><span>❯</span> {code}</code></article>)}</div>
      </section>

      <section className="section shell stack-section" id="stack">
        <div className="stack-intro"><p className="kicker">The recommended path</p><h2>One stack.<br /><span>Chosen well.</span></h2><p>Not the only way to build with .NET. The clearest way to start today.</p></div>
        <div className="stack-list">{stack.map(([label, product, description], index) => <div className="stack-row" key={label}><span className="stack-index">0{index + 1}</span><span className="stack-label">{label}</span><strong>{product}</strong><span className="stack-description">{description}</span><ArrowRight size={18} /></div>)}</div>
      </section>

      <section className="community" id="community"><div className="shell community-grid">
        <div><div className="eyebrow"><Heart size={14} /> Built in the open</div><h2>Made by people<br />who build with <span>.NET.</span></h2><p>This is not another corporate documentation portal. It is a community-owned front door: practical, current, and easy to improve.</p><div className="community-actions"><a className="button primary" href={repositoryUrl} target="_blank" rel="noreferrer">Contribute on GitHub <GitFork size={17} /></a><a className="text-link" href="#principles">Read our principles <ArrowRight size={16} /></a></div></div>
        <div className="contribution-card" id="contribute"><div className="contribution-icon"><PackageOpen size={27} /></div><h3>Documentation belongs to everyone.</h3><p>Every guide is a Markdown file. Fix a typo, improve an explanation, or propose a better path with a pull request.</p><div className="file-tree" aria-label="Documentation file structure"><p><Boxes size={15} /> docs/</p><p><span>├─</span> getting-started.md</p><p><span>├─</span> web-apis.md</p><p><span>├─</span> data.md</p><p><span>└─</span> deployment.md</p></div></div>
      </div></section>

      <section className="principles shell" id="principles"><p>Opinionated, not exclusive.</p><i /><p>Practical, not theoretical.</p><i /><p>Community first.</p></section>
      <footer className="footer shell"><a className="brand" href="#top"><span className="brand-mark">.n</span><span>dotnet<span className="brand-accent">start</span></span></a><p>Making the first step into .NET feel as good as the thousandth.</p><span>Made with C# and unreasonable optimism.</span></footer>
    </main>
  );
}
