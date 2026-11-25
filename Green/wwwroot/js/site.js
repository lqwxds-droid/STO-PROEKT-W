/* ============================================================================
   GREEN AUTO - CLEAN MINIMAL JAVASCRIPT
   Only page load animation - nothing else!
   ============================================================================ */

document.addEventListener('DOMContentLoaded', function() {
  // Apply page load animation to main content
  const contentArea = document.querySelector('.content-area');
  if (contentArea) {
    contentArea.classList.add('page-load');
  }

  // Set active nav link based on current URL
  const normalizePath = path => path.replace(/\/+$/, '').toLowerCase() || '/';
  const currentPath = normalizePath(window.location.pathname);
  const navLinks = document.querySelectorAll('.nav-link');

  navLinks.forEach(link => {
    const href = link.getAttribute('href');
    if (href && normalizePath(new URL(href, window.location.origin).pathname) === currentPath) {
      link.classList.add('active');
    }
  });

  // Sidebar toggle for mobile
  const sidebarToggle = document.getElementById('sidebarToggle');
  if (sidebarToggle) {
    const savedState = localStorage.getItem('green-sidebar-collapsed') === 'true';
    if (window.innerWidth >= 992 && savedState) {
      setSidebarCollapsed(true);
    }

    sidebarToggle.addEventListener('click', function() {
      const sidebar = document.querySelector('.sidebar');
      if (sidebar) {
        if (window.innerWidth >= 992) {
          setSidebarCollapsed(!document.body.classList.contains('sidebar-collapsed'));
        } else {
          const isOpen = sidebar.classList.toggle('show');
          sidebarToggle.setAttribute('aria-expanded', String(isOpen));
          document.body.classList.toggle('sidebar-open', isOpen);
        }
      }
    });
  }

  const sidebarBackdrop = document.querySelector('.sidebar-backdrop');
  if (sidebarBackdrop) {
    sidebarBackdrop.addEventListener('click', closeSidebar);
  }

  document.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
      closeSidebar();
    }
  });

  // Close sidebar when clicking outside on mobile
  document.addEventListener('click', function(event) {
    const sidebar = document.querySelector('.sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');

    if (sidebar && sidebarToggle && !sidebar.contains(event.target) && !sidebarToggle.contains(event.target)) {
      if (window.innerWidth < 992) {
        closeSidebar();
      }
    }
  });

  const actionToast = document.querySelector('.action-toast');
  if (actionToast) {
    const dismissToast = () => {
      actionToast.classList.add('action-toast-hidden');
      setTimeout(() => actionToast.remove(), 250);
    };

    actionToast.querySelector('.action-toast-close')?.addEventListener('click', dismissToast);
    setTimeout(dismissToast, 3500);
  }

  // Auto-dismiss validation and informational alerts after 5 seconds
  // Исключаем блоки с классом working-hours-alert (они должны оставаться всегда видимыми)
  document.querySelectorAll('.alert:not(.working-hours-alert)').forEach(alert => {
    setTimeout(() => alert.remove(), 5000);
  });

  // Form validation styling
  const forms = document.querySelectorAll('form');
  forms.forEach(form => {
    form.addEventListener('submit', function() {
      // Add any form validation here if needed
    });
  });
});

function setSidebarCollapsed(collapsed) {
  document.body.classList.toggle('sidebar-collapsed', collapsed);
  localStorage.setItem('green-sidebar-collapsed', String(collapsed));

  const sidebarToggle = document.getElementById('sidebarToggle');
  if (sidebarToggle) {
    sidebarToggle.setAttribute('aria-expanded', String(!collapsed));
    sidebarToggle.setAttribute('aria-label', collapsed ? 'Развернуть боковую панель' : 'Свернуть боковую панель');
    const icon = sidebarToggle.querySelector('i');
    if (icon) {
      icon.className = collapsed ? 'bi bi-layout-sidebar-inset-reverse' : 'bi bi-layout-sidebar-inset';
    }
  }
}

function closeSidebar() {
  const sidebar = document.querySelector('.sidebar');
  const sidebarToggle = document.getElementById('sidebarToggle');

  if (sidebar) {
    sidebar.classList.remove('show');
  }

  if (sidebarToggle) {
    sidebarToggle.setAttribute('aria-expanded', 'false');
  }

  document.body.classList.remove('sidebar-open');
}

// Handle responsive sidebar on resize
window.addEventListener('resize', function() {
  if (window.innerWidth >= 992) {
    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
      sidebar.classList.remove('show');
    }
    document.body.classList.remove('sidebar-open');
    setSidebarCollapsed(document.body.classList.contains('sidebar-collapsed'));
  } else if (window.innerWidth < 992) {
    document.body.classList.remove('sidebar-collapsed');
  }
});
