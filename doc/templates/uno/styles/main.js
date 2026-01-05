document.addEventListener("DOMContentLoaded",function(){initializeNavbar();var e=document.querySelector("#sdk-version-info"),t=document.querySelector("article h1:first-child");e&&t&&t.appendChild(e);const o="uno_sdk_versions";t=localStorage.getItem(o);let n=null;if(t)try{n=JSON.parse(t),Date.now()-n.timestamp<36e5?s(n.stable,n.dev):n=null}catch(e){n=null}function s(o,n){var e=document.querySelector(".sdk-version"),t=document.querySelector(".sdk-version-badge");e&&(e.textContent=o,t)&&(e=t.cloneNode(!0),t.parentNode.replaceChild(e,t),e.title=`Latest stable: ${o}
Latest dev: ${n}
Click to see update instructions`,e.style.cursor="pointer",e.addEventListener("click",function(e){e.preventDefault();e=document.getElementById("sdk-update-modal");if(e)e.remove();else{const t=document.createElement("div");t.id="sdk-update-modal",t.innerHTML=`
                        <div class="sdk-modal-overlay">
                            <div class="sdk-modal-content">
                                <div class="sdk-modal-header">
                                    <h3>Update Uno SDK</h3>
                                    <button class="sdk-modal-close">&times;</button>
                                </div>
                                <div class="sdk-modal-body">
                                    <div class="sdk-version-section">
                                        <h4>📦 Latest Stable Version</h4>
                                        <p class="version-label"><strong>${o}</strong> - Recommended for production</p>
                                        <div class="sdk-code-block">
                                            <code>dotnet new install Uno.Templates::${o}</code>
                                            <button class="sdk-copy-btn" data-clipboard="dotnet new install Uno.Templates::${o}">Copy</button>
                                        </div>
                                    </div>
                                    
                                    <div class="sdk-version-section">
                                        <h4>🚀 Latest Dev Version</h4>
                                        <p class="version-label"><strong>${n}</strong> - Preview features & fixes</p>
                                        <div class="sdk-code-block">
                                            <code>dotnet new install Uno.Templates::${n}</code>
                                            <button class="sdk-copy-btn" data-clipboard="dotnet new install Uno.Templates::${n}">Copy</button>
                                        </div>
                                        <p class="sdk-note">
                                            📍 <strong>NuGet Package:</strong> <a href="https://www.nuget.org/packages/Uno.Templates" target="_blank" rel="noopener">Uno.Templates on NuGet.org</a>
                                        </p>
                                    </div>
                                    
                                    <div class="sdk-version-section">
                                        <h4>ℹ️ Check Installed Version</h4>
                                        <div class="sdk-code-block">
                                            <code>dotnet new details Uno.Templates</code>
                                            <button class="sdk-copy-btn" data-clipboard="dotnet new details Uno.Templates">Copy</button>
                                        </div>
                                    </div>
                                    
                                    <p class="sdk-note">
                                        💡 <strong>Tip:</strong> If you have an older version installed, uninstall it first using:<br>
                                        <code>dotnet new uninstall Uno.Templates</code>
                                    </p>
                                </div>
                            </div>
                        </div>
                    `,document.body.appendChild(t),t.querySelector(".sdk-modal-close").addEventListener("click",()=>t.remove()),t.querySelector(".sdk-modal-overlay").addEventListener("click",e=>{e.target.classList.contains("sdk-modal-overlay")&&t.remove()}),t.querySelectorAll(".sdk-copy-btn").forEach(e=>{e.addEventListener("click",function(){var e=this.getAttribute("data-clipboard");navigator.clipboard.writeText(e).then(()=>{const e=this.textContent;this.textContent="Copied!",this.classList.add("copied"),setTimeout(()=>{this.textContent=e,this.classList.remove("copied")},2e3)})})})}}))}fetch("https://api.nuget.org/v3-flatcontainer/uno.templates/index.json").then(e=>e.json()).then(e=>{var t;e.versions&&(t=(t=(e=e.versions).filter(e=>!e.includes("-dev")))[t.length-1],e=(e=e.filter(e=>e.includes("-dev")))[e.length-1],localStorage.setItem(o,JSON.stringify({stable:t,dev:e,timestamp:Date.now()})),s(t,e))}).catch(e=>{console.log("Could not fetch SDK version:",e)}),document.addEventListener("click",function(e){var t=e.target;1024<=window.innerWidth||!t.matches("#navbar .menu-item-has-children a")||(e.stopImmediatePropagation(),t.parentElement.classList.toggle("open"))},!0)},!1);
//# sourceMappingURL=main.js.map
